using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Extensions;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Default Model Builder implementation (DD4T-based).
    /// </summary>
    /// <remarks>
    /// Typically this model builder is the only one in the <see cref="ModelBuilderPipeline"/>, but advanced modules (like the SmartTarget module)
    /// may add their own model builder to the pipeline (to post-process the resulting Strongly Typed View Models).
    /// Note that the default model building creates the View Models and ignores any existing ones so should normally be the first in the pipeline.
    /// </remarks>
    public class DefaultModelBuilder : BaseModelBuilder, IModelBuilder
    {
        // TODO: while it works perfectly well, this class is in need of some refactoring to make its behaviour a bit more understandable and maintainable,
        // as its currently very easy to get lost in the semantic mapping logic

        private const string IncludePageCacheRegionName = "PageModel";
        private const string StandardMetadataXmlFieldName = "standardMeta";
        private const string StandardMetadataTitleXmlFieldName = "name";
        private const string StandardMetadataDescriptionXmlFieldName = "description";
        private const string RegionForPageTitleComponent = "Main";
        private const string ComponentXmlFieldNameForPageTitle = "headline";

        #region IModelBuilder members

        public virtual void BuildPageModel(ref PageModel pageModel, IPage page, IEnumerable<IPage> includes, Localization localization)
        {
            using (new Tracer(pageModel, page, includes, localization))
            {
                pageModel = CreatePageModel(page, localization);
                RegionModelSet regions = pageModel.Regions;

                // Create predefined Regions from Page Template Metadata
                CreatePredefinedRegions(regions, page.PageTemplate);

                // Create Regions/Entities from Component Presentations
                foreach (IComponentPresentation cp in page.ComponentPresentations)
                {
                    MvcData cpRegionMvcData = GetRegionMvcData(cp);
                    string regionName = cpRegionMvcData.RegionName ?? cpRegionMvcData.ViewName;
                    RegionModel region;
                    if (regions.TryGetValue(regionName, out region))
                    {
                        // Region already exists in Page Model; MVC data should match.
                        if (!region.MvcData.Equals(cpRegionMvcData))
                        {
                            Log.Warn("Region '{0}' is defined with conflicting MVC data: [{1}] and [{2}]. Using the former.", region.Name, region.MvcData, cpRegionMvcData);
                        }
                    }
                    else
                    {
                        // Region does not exist in Page Model yet; create Region Model and add it.
                        region = CreateRegionModel(cpRegionMvcData);
                        regions.Add(region);
                    }

                    try
                    {
                        EntityModel entity = ModelBuilderPipeline.CreateEntityModel(cp, localization);
                            region.Entities.Add(entity);
                    }
                    catch (Exception ex)
                    {
                        // If there is a problem mapping an Entity, we replace it with an ExceptionEntity which holds the error details and carry on.
                        Log.Error(ex);
                        region.Entities.Add(new ExceptionEntity(ex));
                    }
                }

                // Create Regions from Include Pages
                if (includes != null)
                {
                    foreach (IPage includePage in includes)
                    {
                        PageModel includePageModel = SiteConfiguration.CacheProvider.GetOrAdd(
                            includePage.Id,
                            IncludePageCacheRegionName,
                            () => ModelBuilderPipeline.CreatePageModel(includePage, null, localization),
                            dependencies: new [] { includePage.Id }
                            );

                        // Model Include Page as Region:
                        RegionModel includePageRegion = GetRegionFromIncludePage(includePage);
                        RegionModel existingRegion;
                        if (regions.TryGetValue(includePageRegion.Name, out existingRegion))
                        {
                            // Region with same name already exists; merge include Page Region.
                            existingRegion.Regions.UnionWith(includePageModel.Regions);

                            if (existingRegion.XpmMetadata != null)
                            {
                                existingRegion.XpmMetadata.Remove(RegionModel.IncludedFromPageIdXpmMetadataKey);
                                existingRegion.XpmMetadata.Remove(RegionModel.IncludedFromPageTitleXpmMetadataKey);
                                existingRegion.XpmMetadata.Remove(RegionModel.IncludedFromPageFileNameXpmMetadataKey);
                            }

                            Log.Info("Merged Include Page [{0}] into Region [{1}]. Note that merged Regions can't be edited properly in XPM (yet).",
                                includePageModel, existingRegion);
                        }
                        else
                        {
                            includePageRegion.Regions.UnionWith(includePageModel.Regions);
                            regions.Add(includePageRegion);
                        }

#pragma warning disable 618
                        // Legacy WebPage.Includes support:
                        pageModel.Includes.Add(includePage.Title, includePageModel);
#pragma warning restore 618
                    }

                    if (pageModel.MvcData.ViewName != "IncludePage")
                    {
                        pageModel.Title = ProcessPageMetadata(page, pageModel.Meta, localization);
                    }
                }
            }
        }

        public virtual void BuildEntityModel(ref EntityModel entityModel, IComponentPresentation cp, Localization localization)
        {
            using (new Tracer(entityModel, cp, localization))
            {
                MvcData mvcData = GetMvcData(cp);
                Type modelType = ModelTypeRegistry.GetViewModelType(mvcData);

                // NOTE: not using ModelBuilderPipeline here, but directly calling our own implementation.
                BuildEntityModel(ref entityModel, cp.Component, modelType, localization);

                entityModel.XpmMetadata = GetXpmMetadata(cp.Component);
                entityModel.XpmMetadata.Add("ComponentTemplateID", cp.ComponentTemplate.Id);
                entityModel.XpmMetadata.Add("ComponentTemplateModified", cp.ComponentTemplate.RevisionDate.ToString("yyyy-MM-ddTHH:mm:ss"));
                entityModel.XpmMetadata.Add("IsRepositoryPublished", cp.IsDynamic);
                entityModel.MvcData = mvcData;

                // add html classes to model from metadata
                // TODO: move to CreateViewModel so it can be merged with the same code for a Page/PageTemplate
                IComponentTemplate template = cp.ComponentTemplate;
                if (template.MetadataFields != null && template.MetadataFields.ContainsKey("htmlClasses"))
                {
                    // strip illegal characters to ensure valid html in the view (allow spaces for multiple classes)
                    entityModel.HtmlClasses = template.MetadataFields["htmlClasses"].Value.StripIllegalCharacters(@"[^\w\-\ ]");
                }

                if (cp.IsDynamic)
                {
                    // update Entity Identifier to that of a DCP
                    entityModel.Id = GetDxaIdentifierFromTcmUri(cp.Component.Id, cp.ComponentTemplate.Id);
                }
            }
        }

        public virtual void BuildEntityModel(ref EntityModel entityModel, IComponent component, Type baseModelType, Localization localization)
        {
            using (new Tracer(entityModel, component, baseModelType, localization))
            {
                string[] schemaTcmUriParts = component.Schema.Id.Split('-');
                SemanticSchema semanticSchema = SemanticMapping.GetSchema(schemaTcmUriParts[1], localization);

                // The semantic mapping may resolve to a more specific model type than specified by the View Model itself (e.g. Image instead of just MediaItem for Teaser.Media)
                Type modelType = semanticSchema.GetModelTypeFromSemanticMapping(baseModelType);

                MappingData mappingData = new MappingData
                {
                    SemanticSchema = semanticSchema,
                    EntityNames = semanticSchema.GetEntityNames(),
                    TargetEntitiesByPrefix = GetEntityDataFromType(modelType),
                    Content = component.Fields,
                    Meta = component.MetadataFields,
                    TargetType = modelType,
                    SourceEntity = component,
                    Localization = localization
                };

                entityModel = (EntityModel)CreateViewModel(mappingData);
                entityModel.Id = GetDxaIdentifierFromTcmUri(component.Id);

                if (entityModel is MediaItem && component.Multimedia != null && component.Multimedia.Url != null)
                {
                    MediaItem mediaItem = (MediaItem)entityModel;
                    mediaItem.Url = component.Multimedia.Url;
                    mediaItem.FileName = component.Multimedia.FileName;
                    mediaItem.FileSize = component.Multimedia.Size;
                    mediaItem.MimeType = component.Multimedia.MimeType;
                }

                if (entityModel is EclItem)
                {
                    MapEclItem((EclItem) entityModel, component);
                }

                if (entityModel is Link)
                {
                    Link link = (Link)entityModel;
                    if (String.IsNullOrEmpty(link.Url))
                    {
                        link.Url = SiteConfiguration.LinkResolver.ResolveLink(component.Id);
                    }
                }

                // Set the Entity Model's default View (if any) after it has been fully initialized.
                entityModel.MvcData = entityModel.GetDefaultView(localization);
            }
        }
            
        #endregion

        private static void MapEclItem(EclItem eclItem, IComponent component)
        {
            eclItem.EclUri = component.EclId;

            IFieldSet eclExtensionDataFields;
            if (component.ExtensionData == null || !component.ExtensionData.TryGetValue("ECL", out eclExtensionDataFields))
            {
                Log.Warn("Encountered ECL Stub Component without ECL Extension Data: {0}", component.Id);
                return;
            }

            eclItem.EclDisplayTypeId = GetFieldValue("DisplayTypeId", eclExtensionDataFields);
            eclItem.EclTemplateFragment = GetFieldValue("TemplateFragment", eclExtensionDataFields);
            string eclFileName = GetFieldValue("FileName", eclExtensionDataFields);
            if (!string.IsNullOrEmpty(eclFileName))
            {
                eclItem.FileName = eclFileName;
            }
            string eclMimeType = GetFieldValue("MimeType", eclExtensionDataFields);
            if (!string.IsNullOrEmpty(eclMimeType))
            {
                eclItem.MimeType = eclMimeType;
            }

            IFieldSet eclExternalMetadataFields;
            if (component.ExtensionData.TryGetValue("ECL-ExternalMetadata", out eclExternalMetadataFields))
            {
                eclItem.EclExternalMetadata = MapEclExternalMetadata(eclExternalMetadataFields);
            }
        }


        private static IDictionary<string, object> MapEclExternalMetadata(IFieldSet fields)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            foreach (IField field in fields.Values)
            {
                object mappedValue;
                switch (field.FieldType)
                {
                    case FieldType.Number:
                        mappedValue = GetFieldValue(field.NumericValues);
                        break;

                    case FieldType.Date:
                        mappedValue = GetFieldValue(field.DateTimeValues);
                        break;

                    case FieldType.Embedded:
                        if (field.EmbeddedValues.Count == 1)
                        {
                            mappedValue = MapEclExternalMetadata(field.EmbeddedValues[0]);
                        }
                        else
                        {
                            mappedValue = field.EmbeddedValues.Select(MapEclExternalMetadata).ToArray();
                        }
                        break;

                    default:
                        mappedValue = GetFieldValue(field.Values);
                        break;
                }
                result.Add(field.Name, mappedValue);
            }
            return result;
        }

        private static object GetFieldValue<T>(IList<T> fieldValues)
        {
            switch (fieldValues.Count)
            {
                case 0:
                    return null;
                case 1:
                    return fieldValues[0];
                default:
                    return fieldValues;
            }
        }

        private static string GetFieldValue(string fieldName, IFieldSet fields)
        {
            IField field;
            if (!fields.TryGetValue(fieldName, out field))
            {
                return null;
            }
            return field.Value;
        }

        private PageModel CreatePageModel(IPage page, Localization localization)
        {
            MvcData pageMvcData = GetMvcData(page);
            Type pageModelType = ModelTypeRegistry.GetViewModelType(pageMvcData);
            string pageId = GetDxaIdentifierFromTcmUri(page.Id);
            ISchema pageMetadataSchema = page.Schema;

            PageModel pageModel;
            if (pageModelType == typeof(PageModel))
            {
                // Standard Page Model
                pageModel = new PageModel(pageId);
            }
            else if (pageMetadataSchema == null)
            {
                // Custom Page Model but no Page metadata that can be mapped; simply create a Page Model instance of the right type.
                pageModel = (PageModel)Activator.CreateInstance(pageModelType, pageId);
            }
            else
            {
                // Custom Page Model and Page metadata is present; do full-blown model mapping.
                string[] schemaTcmUriParts = pageMetadataSchema.Id.Split('-');
                SemanticSchema semanticSchema = SemanticMapping.GetSchema(schemaTcmUriParts[1], localization);

                MappingData mappingData = new MappingData
                {
                    TargetType = pageModelType,
                    SemanticSchema = semanticSchema,
                    EntityNames = semanticSchema.GetEntityNames(),
                    TargetEntitiesByPrefix = GetEntityDataFromType(pageModelType),
                    Meta = page.MetadataFields,
                    ModelId = pageId,
                    Localization = localization
                };

                pageModel = (PageModel) CreateViewModel(mappingData);
            }

            pageModel.MvcData = pageMvcData;
            pageModel.XpmMetadata = GetXpmMetadata(page);
            pageModel.Title = page.Title;

            // add html classes to model from metadata
            // TODO: move to CreateViewModel so it can be merged with the same code for a Component/ComponentTemplate
            IPageTemplate template = page.PageTemplate;
            if (template.MetadataFields != null && template.MetadataFields.ContainsKey("htmlClasses"))
            {
                // strip illegal characters to ensure valid html in the view (allow spaces for multiple classes)
                pageModel.HtmlClasses = template.MetadataFields["htmlClasses"].Value.StripIllegalCharacters(@"[^\w\-\ ]");
            }

            return pageModel;
        }


        protected virtual ViewModel CreateViewModel(MappingData mappingData)
        {
            Type modelType = mappingData.TargetType;

            ViewModel model;
            if (string.IsNullOrEmpty(mappingData.ModelId))
            {
                // Use parameterless constructor
                model = (ViewModel)Activator.CreateInstance(modelType);
            }
            else
            {
                // Pass model Identifier in constructor.
                model = (ViewModel)Activator.CreateInstance(modelType, mappingData.ModelId);
            }

            Dictionary<string, string> xpmPropertyMetadata = new Dictionary<string, string>();
            Dictionary<string, List<SemanticProperty>> propertySemantics = LoadPropertySemantics(modelType);
            propertySemantics = FilterPropertySemanticsByEntity(propertySemantics, mappingData);

            foreach (PropertyInfo modelProperty in modelType.GetProperties())
            {
                if (!propertySemantics.ContainsKey(modelProperty.Name))
                {
                    continue;
                }

                Type modelPropertyType = modelProperty.PropertyType;
                bool isCollection = modelPropertyType.IsGenericType && (modelPropertyType.GetGenericTypeDefinition() == typeof(List<>));
                Type valueType = isCollection ? modelPropertyType.GetGenericArguments()[0] : modelPropertyType;

                foreach (SemanticProperty semanticProperty in propertySemantics[modelProperty.Name])
                {
                    SemanticSchemaField semanticSchemaField;
                    IField dd4tField = GetFieldFromSemantics(mappingData, semanticProperty, out semanticSchemaField);
                    if (dd4tField != null)
                    {
                        object propertyValue = MapFieldValues(dd4tField, valueType, isCollection, mappingData, semanticSchemaField);
                        if (propertyValue != null)
                        {
                            modelProperty.SetValue(model, propertyValue);
                        }
                        xpmPropertyMetadata.Add(modelProperty.Name, dd4tField.XPath);
                        break;
                    }

                    // Special mapping cases require SourceEntity to be set
                    if (mappingData.SourceEntity == null)
                    {
                        continue;
                    }

                    bool processed = false;
                    if (semanticProperty.PropertyName == "_self")
                    {
                        //Map the whole entity to an image property, or a resolved link to the entity to a Url field
                        if (typeof(MediaItem).IsAssignableFrom(valueType) || typeof(Link).IsAssignableFrom(valueType) || valueType == typeof(string))
                        {
                            object mappedSelf = MapComponent(mappingData.SourceEntity, valueType, mappingData.Localization);
                            if (isCollection)
                            {
                                IList genericList = CreateGenericList(valueType);
                                genericList.Add(mappedSelf);
                                modelProperty.SetValue(model, genericList);
                            }
                            else
                            {
                                modelProperty.SetValue(model, mappedSelf);
                            }
                            processed = true;
                        }
                    }
                    else if (semanticProperty.PropertyName == "_all" && modelProperty.PropertyType == typeof(Dictionary<string, string>))
                    {
                        //Map all fields into a single (Dictionary) property
                        modelProperty.SetValue(model, GetAllFieldsAsDictionary(mappingData.SourceEntity));
                        processed = true;
                    }

                    if (processed)
                    {
                        break;
                    }
                }
            }

            EntityModel entityModel = model as EntityModel;
            if (entityModel != null)
            {
                entityModel.XpmPropertyMetadata = xpmPropertyMetadata;
            }

            return model;
        }

        protected virtual Dictionary<string, List<SemanticProperty>> FilterPropertySemanticsByEntity(Dictionary<string, List<SemanticProperty>> propertySemantics, MappingData mapData)
        {
            Dictionary<string, List<SemanticProperty>> filtered = new Dictionary<string, List<SemanticProperty>>();
            foreach (KeyValuePair<string, List<SemanticProperty>> property in propertySemantics)
            {
                filtered.Add(property.Key, new List<SemanticProperty>());
                List<SemanticProperty> defaultProperties = new List<SemanticProperty>();
                foreach (SemanticProperty semanticProperty in property.Value)
                {
                    //Default prefix is always OK, but should be added last
                    if (string.IsNullOrEmpty(semanticProperty.Prefix))
                    {
                        defaultProperties.Add(semanticProperty);
                    }
                    else
                    {
                        //Filter out any properties belonging to other entities than the source entity
                        KeyValuePair<string, string>? entityData = GetEntityData(semanticProperty.Prefix, mapData.TargetEntitiesByPrefix, mapData.ParentDefaultPrefix);
                        if (entityData != null && mapData.EntityNames!=null && mapData.EntityNames.Contains(entityData.Value.Key))
                        {
                            if (mapData.EntityNames[entityData.Value.Key].First() == entityData.Value.Value)
                            {
                                filtered[property.Key].Add(semanticProperty);
                            }
                        }
                    }
                }
                filtered[property.Key].AddRange(defaultProperties);
            }
            return filtered;
        }

        private static IField GetFieldFromSemantics(MappingData mapData, SemanticProperty info, out SemanticSchemaField semanticSchemaField)
        {
            KeyValuePair<string, string>? entityData = GetEntityData(info.Prefix, mapData.TargetEntitiesByPrefix, mapData.ParentDefaultPrefix);
            if (entityData != null)
            {
                // determine field semantics
                string vocab = entityData.Value.Key;
                string prefix = SemanticMapping.GetPrefix(vocab, mapData.Localization);
                if (prefix != null && mapData.EntityNames!=null)
                {
                    string property = info.PropertyName;
                    string entity = mapData.EntityNames[vocab].FirstOrDefault();
                    if (entity != null && mapData.SemanticSchema!=null)
                    {
                        FieldSemantics fieldSemantics = new FieldSemantics(prefix, entity, property);
                        // locate semantic schema field
                        semanticSchemaField = (mapData.EmbeddedSemanticSchemaField == null) ? 
                            mapData.SemanticSchema.FindFieldBySemantics(fieldSemantics) : // Used for top-level fields
                            mapData.EmbeddedSemanticSchemaField.FindFieldBySemantics(fieldSemantics); // Used for embedded fields
                        if (semanticSchemaField != null)
                        {
                            return ExtractMatchedField(semanticSchemaField, (semanticSchemaField.IsMetadata && mapData.Meta!=null) ? mapData.Meta : mapData.Content, mapData.EmbedLevel);
                        }
                    }
                }
            }

            semanticSchemaField = null;
            return null;
        }

        private static KeyValuePair<string, string>? GetEntityData(string prefix, Dictionary<string, KeyValuePair<string, string>> entityData, string defaultPrefix)
        {
            if (defaultPrefix != null && String.IsNullOrEmpty(prefix))
            {
                prefix = defaultPrefix;
            }
            if (entityData.ContainsKey(prefix))
            {
                return entityData[prefix];
            }
            return null;
        }


        private static IField ExtractMatchedField(SemanticSchemaField semanticField, IFieldSet fields, int embedLevel)
        {
            // Split the path in segments. The first segment represents the root element name.
            string[] pathSegments = semanticField.Path.Split(new [] {'/'}, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length < embedLevel + 2)
            {
                throw new DxaException(
                    string.Format("Semantic field path '{0}' is too short for the current embed level: {1}.", semanticField.Path, embedLevel)
                    );
            }
            string currentPathSegment = pathSegments[embedLevel + 1];

            IField matchedField;
            if (!fields.TryGetValue(currentPathSegment, out matchedField))
            {
                // No matching field found.
                return null;
            }

            if (pathSegments.Length > embedLevel + 2)
            {
                // The semantic field is within an embedded field; flatten the structure.
                matchedField = ExtractMatchedField(semanticField, matchedField.EmbeddedValues[0], embedLevel + 1);
            }

            return matchedField;
        }


        private static IList CreateGenericList(Type listItemType)
        {
            ConstructorInfo genericListConstructor = typeof(List<>).MakeGenericType(listItemType).GetConstructor(Type.EmptyTypes);
            if (genericListConstructor == null)
            {
                // This should never happen.
                throw new DxaException(String.Format("Unable get constructor for generic list of '{0}'.", listItemType.FullName));
            }

            return (IList)genericListConstructor.Invoke(null);
        }


        private object MapFieldValues(IField field, Type modelType, bool multival, MappingData mapData, SemanticSchemaField semanticSchemaField)
        {
            try
            {
                // Convert.ChangeType cannot convert non-nullable types to nullable types, so don't try that.
                Type bareModelType = modelType;
                if (modelType.IsGenericType && modelType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    bareModelType = modelType.GenericTypeArguments[0];
                }

                IList mappedValues = CreateGenericList(modelType);
                switch (field.FieldType)
                {
                    case FieldType.Date:
                        foreach (DateTime value in field.DateTimeValues)
                        {
                            mappedValues.Add(Convert.ChangeType(value, bareModelType));
                        }
                        break;

                    case FieldType.Number:
                        foreach (Double value in field.NumericValues)
                        {
                            mappedValues.Add(Convert.ChangeType(value, bareModelType));
                        }
                        break;

                    case FieldType.MultiMediaLink:
                    case FieldType.ComponentLink:
                        foreach (IComponent value in field.LinkedComponentValues)
                        {
                            mappedValues.Add(MapComponent(value, modelType, mapData.Localization));
                        }
                        break;

                    case FieldType.Embedded:
                        foreach (IFieldSet value in field.EmbeddedValues)
                        {
                            mappedValues.Add(MapEmbeddedFields(value, modelType, mapData, semanticSchemaField));
                        }
                        break;

                    case FieldType.Keyword:
                        foreach (IKeyword value in field.Keywords)
                        {
                            mappedValues.Add(MapKeyword(value, modelType));
                        }
                        break;

                    case FieldType.Xhtml:
                        IRichTextProcessor richTextProcessor = SiteConfiguration.RichTextProcessor;
                        foreach (string value in field.Values)
                        {
                            RichText richText = richTextProcessor.ProcessRichText(value, mapData.Localization);
                            if (modelType == typeof(string))
                            {
                                mappedValues.Add(richText.ToString());
                            }
                            else
                            {
                                mappedValues.Add(richText);
                            }
                        }
                        break;

                    default:
                        foreach (string value in field.Values)
                        {
                            object mappedValue = (modelType == typeof(RichText)) ? new RichText(value) : Convert.ChangeType(value, bareModelType);
                            mappedValues.Add(mappedValue);
                        }
                        break;
                }

                if (multival)
                {
                    return mappedValues;
                }
                
                return mappedValues.Count == 0 ? null : mappedValues[0];

            }
            catch (Exception ex)
            {
                throw new DxaException(String.Format("Unable to map field '{0}' to property of type '{1}'.", field.Name, modelType.FullName), ex);
            }
        }

        protected virtual IDictionary<string, object> GetXpmMetadata(IComponent comp)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            if (comp != null)
            {
                result.Add("ComponentID", comp.Id);
                result.Add("ComponentModified", comp.RevisionDate.ToString("yyyy-MM-ddTHH:mm:ss"));
            }
            return result;
        }

        protected virtual IDictionary<string, object> GetXpmMetadata(IPage page)
        {
            IDictionary<string, object> result = new Dictionary<string, object>();
            if (page != null)
            {
                result.Add("PageID", page.Id);
                result.Add("PageModified", page.RevisionDate.ToString("yyyy-MM-ddTHH:mm:ss"));
                result.Add("PageTemplateID", page.PageTemplate.Id);
                result.Add("PageTemplateModified", page.PageTemplate.RevisionDate.ToString("yyyy-MM-ddTHH:mm:ss"));
            }
            return result;
        }

        protected virtual object MapKeyword(IKeyword keyword, Type modelType)
        {
            // TODO TSI-811: Keyword mapping should also be generic rather than hard-coded like below
            string displayText = String.IsNullOrEmpty(keyword.Description) ? keyword.Title : keyword.Description;
            if (modelType == typeof(Tag))
            {
                return new Tag
                {
                    DisplayText = displayText,
                    Key = String.IsNullOrEmpty(keyword.Key) ? keyword.Id : keyword.Key,
                    TagCategory = keyword.TaxonomyId
                };
            } 
            
            if (modelType == typeof(bool))
            {
                //For booleans we assume the keyword key or value can be converted to bool
                return Boolean.Parse(String.IsNullOrEmpty(keyword.Key) ? keyword.Title : keyword.Key);
            }
            
            if (modelType == typeof(string))
            {
                return displayText;
            }

            throw new DxaException(String.Format("Cannot map Keyword to type '{0}'. The type must be Tag, bool or string.", modelType));
        }

        protected virtual object MapComponent(IComponent component, Type modelType, Localization localization)
        {
            if (modelType == typeof(string))
            {
                return SiteConfiguration.LinkResolver.ResolveLink(component.Id);
            }

            if (!modelType.IsSubclassOf(typeof(EntityModel)))
            {
                throw new DxaException(String.Format("Cannot map a Component to type '{0}'. The type must be String or a subclass of EntityModel.", modelType));
            }

            return ModelBuilderPipeline.CreateEntityModel(component, modelType, localization);
        }

        private ViewModel MapEmbeddedFields(IFieldSet embeddedFields, Type modelType, MappingData mapData, SemanticSchemaField semanticSchemaField)
        {
            MappingData embeddedMappingData = new MappingData(mapData)
            {
                TargetType = modelType,
                Content = embeddedFields,
                Meta = null,
                EmbeddedSemanticSchemaField = semanticSchemaField,
                EmbedLevel = mapData.EmbedLevel + 1
            };

            EntityModel result = (EntityModel) CreateViewModel(embeddedMappingData);
            result.MvcData = result.GetDefaultView(mapData.Localization);
            return result;
        }

        protected Dictionary<string, string> GetAllFieldsAsDictionary(IComponent component)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (string fieldname in component.Fields.Keys)
            {
                if (!values.ContainsKey(fieldname))
                {
                    //special case for multival embedded name/value pair fields
                    if (fieldname == "settings" && component.Fields[fieldname].FieldType == FieldType.Embedded)
                    {
                        foreach (IFieldSet embedFieldset in component.Fields[fieldname].EmbeddedValues)
                        {
                            string key = embedFieldset.ContainsKey("name") ? embedFieldset["name"].Value : null;
                            if (key != null)
                            {
                                string value = embedFieldset.ContainsKey("value") ? embedFieldset["value"].Value : null;
                                if (!values.ContainsKey(key))
                                {
                                    values.Add(key, value);
                                }
                            }
                        }
                    }
                    //Default is to add the value as plain text
                    else
                    {
                        string val = GetFieldValuesAsStrings(component.Fields[fieldname]).FirstOrDefault();
                        if (val != null)
                        {
                            values.Add(fieldname, val);
                        }
                    }
                }
            }
            foreach (string fieldname in component.MetadataFields.Keys)
            {
                if (!values.ContainsKey(fieldname))
                {
                    string val = GetFieldValuesAsStrings(component.MetadataFields[fieldname]).FirstOrDefault();
                    if (val != null)
                    {
                        values.Add(fieldname, val);
                    }
                }
            }
            return values;
        }

        private static string[] GetFieldValuesAsStrings(IField field)
        {
            switch (field.FieldType)
            {
                case FieldType.Number:
                    return field.NumericValues.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToArray();
                case FieldType.Date:
                    return field.DateTimeValues.Select(v => v.ToString("s")).ToArray();
                case FieldType.ComponentLink:
                case FieldType.MultiMediaLink:
                    return field.LinkedComponentValues.Select(v => v.Id).ToArray();
                case FieldType.Keyword:
                    return field.KeywordValues.Select(v => v.Id).ToArray();
                default:
                    return field.Values.ToArray();
            }
        }


        protected virtual string ProcessPageMetadata(IPage page, IDictionary<string, string> meta, Localization localization)
        {
            //First grab metadata from the page
            if (page.MetadataFields != null)
            {
                foreach (IField field in page.MetadataFields.Values)
                {
                    ProcessMetadataField(field, meta);
                }
            }
            string description = meta.ContainsKey("description") ? meta["description"] : null;
            string title = meta.ContainsKey("title") ? meta["title"] : null;
            string image = meta.ContainsKey("image") ? meta["image"] : null;

            //If we don't have a title or description - go hunting for a title and/or description from the first component in the main region on the page
            if (title == null || description == null)
            {
                bool first = true;
                foreach (IComponentPresentation cp in page.ComponentPresentations)
                {
                    MvcData regionMvcData = GetRegionMvcData(cp);
                    // determine title and description from first component in 'main' region
                    if (first && regionMvcData.ViewName.Equals(RegionForPageTitleComponent))
                    {
                        first = false;
                        IFieldSet metadata = cp.Component.MetadataFields;
                        IFieldSet fields = cp.Component.Fields;
                        if (metadata.ContainsKey(StandardMetadataXmlFieldName) && metadata[StandardMetadataXmlFieldName].EmbeddedValues.Count > 0)
                        {
                            IFieldSet standardMeta = metadata[StandardMetadataXmlFieldName].EmbeddedValues[0];
                            if (title == null && standardMeta.ContainsKey(StandardMetadataTitleXmlFieldName))
                            {
                                title = standardMeta[StandardMetadataTitleXmlFieldName].Value;
                            }
                            if (description == null && standardMeta.ContainsKey(StandardMetadataDescriptionXmlFieldName))
                            {
                                description = standardMeta[StandardMetadataDescriptionXmlFieldName].Value;
                            }
                        }
                        if (title == null && fields.ContainsKey(ComponentXmlFieldNameForPageTitle))
                        {
                            title = fields[ComponentXmlFieldNameForPageTitle].Value;
                        }
                        //Try to find an image
                        if (image == null && fields.ContainsKey("image"))
                        {
                            image = fields["image"].LinkedComponentValues[0].Multimedia.Url;
                        }
                    }
                }
            }
            IDictionary coreResources = localization.GetResources("core");
            string titlePostfix = coreResources["core.pageTitleSeparator"].ToString() + coreResources["core.pageTitlePostfix"].ToString();
            //if we still dont have a title, use the page title
            if (title == null)
            {
                title = Regex.Replace(page.Title, @"^\d{3}\s", String.Empty);
                // Index and Default are not a proper titles for an HTML page
                if (title.ToLowerInvariant().Equals("index") || title.ToLowerInvariant().Equals("default"))
                {
                    title = coreResources["core.defaultPageTitle"].ToString();
                }
            }
            meta.Add("twitter:card", "summary");
            meta.Add("og:title", title);
            // TODO: if the URL is really needed, it should be added higher up (e.g. in the View code):  meta.Add("og:url", WebRequestContext.RequestUrl);
            // TODO: is this always article?
            meta.Add("og:type", "article");
            meta.Add("og:locale", localization.Culture);
            if (description != null)
            {
                meta.Add("og:description", description);
            }
            if (image != null)
            {
                image = localization.GetBaseUrl() + image;
                meta.Add("og:image", image);
            }
            if (!meta.ContainsKey("description"))
            {
                meta.Add("description", description ?? title);
            }
            return title + titlePostfix;
        }

        protected virtual void ProcessMetadataField(IField field, IDictionary<string, string> meta)
        {
            if (field.FieldType==FieldType.Embedded)
            {
                if (field.EmbeddedValues!=null && field.EmbeddedValues.Count>0)
                {
                    IFieldSet subfields = field.EmbeddedValues[0];
                    foreach (IField subfield in subfields.Values)
                    {
                        ProcessMetadataField(subfield, meta);
                    }
                }
            }
            else
            {
                string value;
                switch (field.Name)
                {
                    case "internalLink":
                        value = SiteConfiguration.LinkResolver.ResolveLink(field.Value);
                        break;
                    case "image":
                        value = field.LinkedComponentValues[0].Multimedia.Url;
                        break;
                    default:
                        value = String.Join(",", field.Values);
                        break;
                }
                if (value != null && !meta.ContainsKey(field.Name))
                {
                    meta.Add(field.Name, value);
                }
            }
        }
                
        private static void InitializeRegionMvcData(MvcData regionMvcData)
        {
            if (String.IsNullOrEmpty(regionMvcData.ControllerName))
            {
                regionMvcData.ControllerName = SiteConfiguration.GetRegionController();
                regionMvcData.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
            }
            else if (String.IsNullOrEmpty(regionMvcData.ControllerAreaName))
            {
                regionMvcData.ControllerAreaName = regionMvcData.AreaName;
            }
            regionMvcData.ActionName = SiteConfiguration.GetRegionAction();
        }

        private static MvcData GetRegionMvcData(IComponentPresentation cp)
        {
            IComponentTemplate ct = cp.ComponentTemplate;

            string regionViewName = null;
            IField regionViewNameField;
            if (ct.MetadataFields != null && ct.MetadataFields.TryGetValue("regionView", out regionViewNameField))
            {
                regionViewName = regionViewNameField.Value;
            }
            else
            {
                // Fallback if no CT metadata found: try to extract Region View name from CT title
                if (ct.Title == null)
                {
                    // If a DCP has been published with DDT4 1.31 TBBs, it won't be read properly using the DD4T 2.0 ComponentPresentationFactory
                    // resulting in almost all CT properties being null.
                    throw new DxaException(
                        string.Format("No Component Template data available for DCP '{0}/{1}'. Republish the DCP to ensure it uses the new DD4T 2.0 DCP format.",
                            cp.Component.Id, cp.ComponentTemplate.Id)
                        );
                }
                Match match = Regex.Match(ct.Title, @".*?\[(.+?)\]");
                if (match.Success)
                {
                    regionViewName = match.Groups[1].Value;
                }
            }

            if (String.IsNullOrEmpty(regionViewName))
            {
                regionViewName = "Main";
            }

            MvcData regionMvcData = new MvcData(regionViewName);
            InitializeRegionMvcData(regionMvcData);

            IField regionNameField;
            if (ct.MetadataFields != null && ct.MetadataFields.TryGetValue("regionName", out regionNameField))
            {
                if (!String.IsNullOrEmpty(regionNameField.Value))
                {
                    regionMvcData.RegionName = regionNameField.Value;
                }
            }

            return regionMvcData;
        }

        private static RegionModel GetRegionFromIncludePage(IPage page)
        {
            // Page Title can be a qualified View Name; we use the unqualified View Name as Region Name.
            MvcData regionMvcData = new MvcData(page.Title);
            InitializeRegionMvcData(regionMvcData);

            return new RegionModel(regionMvcData.ViewName)
            {
                MvcData = regionMvcData,
                XpmMetadata = new Dictionary<string, object>
                {
                    {RegionModel.IncludedFromPageIdXpmMetadataKey, page.Id},
                    {RegionModel.IncludedFromPageTitleXpmMetadataKey, page.Title},
                    {RegionModel.IncludedFromPageFileNameXpmMetadataKey, page.Filename}
                }
            };
        }

        /// <summary>
        /// Creates a Region Model of class <see cref="RegionModel"/> or a subclass associated with the given Region View.
        /// </summary>
        private static RegionModel CreateRegionModel(MvcData regionMvcData)
        {
            string regionName = regionMvcData.RegionName ?? regionMvcData.ViewName;

            Type regionModelType = ModelTypeRegistry.GetViewModelType(regionMvcData);
            RegionModel regionModel = (RegionModel) Activator.CreateInstance(regionModelType, regionName);
            regionModel.MvcData = new MvcData(regionMvcData)
            {
                RegionName = null // Suppress RegionName in the final model.
            };

            return regionModel;
        }

        /// <summary>
        /// Creates predefined Regions from Page Template metadata.
        /// </summary>
        private static void CreatePredefinedRegions(RegionModelSet regions, IPageTemplate pageTemplate)
        {
            IFieldSet ptMetadataFields = pageTemplate.MetadataFields;
            IField regionsField;
            if (ptMetadataFields == null || !ptMetadataFields.TryGetValue("regions", out regionsField))
            {
                Log.Debug("No Region metadata defined for Page Template '{0}'.", pageTemplate.Id);
                return;
            }

            foreach (IFieldSet regionMetadataFields in regionsField.EmbeddedValues)
            {
                IField regionViewNameField;
                if (!regionMetadataFields.TryGetValue("view", out regionViewNameField))
                {
                    Log.Warn("Region metadata without 'view' field encountered in metadata of Page Template '{0}'.", pageTemplate.Id);
                    continue;
                }

                MvcData regionMvcData = new MvcData(regionViewNameField.Value);
                InitializeRegionMvcData(regionMvcData);

                IField regionNameField;
                if (regionMetadataFields.TryGetValue("name", out regionNameField) && !String.IsNullOrEmpty(regionNameField.Value))
                {
                    regionMvcData.RegionName = regionNameField.Value;
                }

                RegionModel regionModel = CreateRegionModel(regionMvcData);
                regions.Add(regionModel);
            }
        }


        internal static string GetDxaIdentifierFromTcmUri(string tcmUri, string templateTcmUri = null)
        {
            // Return the Item (Reference) ID part of the TCM URI.
            string result = tcmUri.Split('-')[1];
            if (templateTcmUri != null)
            {
                result += "-" + templateTcmUri.Split('-')[1];
            }
            return result;
        }

        /// <summary>
        /// Determine MVC data such as view, controller and area name from a Page
        /// </summary>
        /// <param name="page">The DD4T Page object</param>
        /// <returns>MVC data</returns>
        private static MvcData GetMvcData(IPage page)
        {
            IPageTemplate pt = page.PageTemplate;

            string viewName;
            IField viewNameField;
            if (pt.MetadataFields != null && pt.MetadataFields.TryGetValue("view", out viewNameField))
            {
                viewName = viewNameField.Value;
            }
            else
            {
                // Fallback if no View name is defined in PT Metadata: get View name from PT Title.
                viewName = pt.Title.RemoveSpaces();
            }

            return new MvcData(viewName)
            {
                ControllerName = SiteConfiguration.GetPageController(),
                ControllerAreaName = SiteConfiguration.GetDefaultModuleName(),
                ActionName = SiteConfiguration.GetPageAction()
            };
        }

        /// <summary>
        /// Determine MVC data such as view, controller and area name from a Component Presentation
        /// </summary>
        /// <param name="cp">The DD4T Component Presentation</param>
        /// <returns>MVC data</returns>
        private static MvcData GetMvcData(IComponentPresentation cp)
        {
            IComponentTemplate ct = cp.ComponentTemplate;

            string viewName;
            IField viewNameField;
            if (ct.MetadataFields != null && ct.MetadataFields.TryGetValue("view", out viewNameField))
            {
                viewName = viewNameField.Value;
            }
            else
            {
                // Fallback if no View name defined in CT Metadata: extract View name from CT Title.
                if (ct.Title == null)
                {
                    // If a DCP has been published with DDT4 1.31 TBBs, it won't be read properly using the DD4T 2.0 ComponentPresentationFactory
                    // resulting in almost all CT properties being null.
                    throw new DxaException(
                        string.Format("No Component Template data available for DCP '{0}/{1}'. Republish the DCP to ensure it uses the new DD4T 2.0 DCP format.",
                            cp.Component.Id, cp.ComponentTemplate.Id)
                        );
                }
                viewName = Regex.Replace(ct.Title, @"\[.*\]|\s", string.Empty);
            }

            MvcData regionMvcData = GetRegionMvcData(cp);

            MvcData mvcData = new MvcData(viewName)
            {
                RegionName = regionMvcData.RegionName ?? regionMvcData.ViewName,
                RegionAreaName = regionMvcData.AreaName,
                // Defaults:
                ControllerName = SiteConfiguration.GetEntityController(),
                ControllerAreaName = SiteConfiguration.GetDefaultModuleName(),
                ActionName = SiteConfiguration.GetEntityAction()
            };

            if (ct.MetadataFields != null)
            {
                if (ct.MetadataFields.ContainsKey("controller"))
                {
                    string[] controllerNameParts = ct.MetadataFields["controller"].Value.Split(':');
                    if (controllerNameParts.Length > 1)
                    {
                        mvcData.ControllerName = controllerNameParts[1];
                        mvcData.ControllerAreaName = controllerNameParts[0];
                    }
                    else
                    {
                        mvcData.ControllerName = controllerNameParts[0];
                    }
                }
                if (ct.MetadataFields.ContainsKey("action"))
                {
                    mvcData.ActionName = ct.MetadataFields["action"].Value;
                }
                if (ct.MetadataFields.ContainsKey("routeValues"))
                {
                    string[] routeValues = ct.MetadataFields["routeValues"].Value.Split(',');
                    mvcData.RouteValues = new Dictionary<string, string>(routeValues.Length);
                    foreach (string routeValue in routeValues)
                    {
                        string[] routeValueParts = routeValue.Trim().Split(':');
                        if (routeValueParts.Length > 1 && !mvcData.RouteValues.ContainsKey(routeValueParts[0]))
                        {
                            mvcData.RouteValues.Add(routeValueParts[0], routeValueParts[1]);
                        }
                    }
                }
            }

            return mvcData;
        }
    }
}
