using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using DD4T.Factories;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Common;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Tridion.Extensions;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Meta;
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

        private const string StandardMetadataXmlFieldName = "standardMeta";
        private const string StandardMetadataTitleXmlFieldName = "name";
        private const string StandardMetadataDescriptionXmlFieldName = "description";
        private const string RegionForPageTitleComponent = "Main";
        private const string ComponentXmlFieldNameForPageTitle = "headline";

        private ResourceProvider _resourceProvider;

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
                IConditionalEntityEvaluator conditionalEntityEvaluator = SiteConfiguration.ConditionalEntityEvaluator;
                foreach (IComponentPresentation cp in page.ComponentPresentations)
                {
                    IComponentPresentation fullyLoadedCp = cp;
                    if (cp.IsDynamic)
                    {
                        // this is a workaround for the PageFactory not populating the Fields property of Dynamic Component Presentations in the Page model
                        fullyLoadedCp = LoadDcp(cp.Component.Id, cp.ComponentTemplate.Id);
                        Log.Debug("Loading DCP {0}, {1}", cp.Component.Id, cp.ComponentTemplate.Id);
                    }


                    MvcData cpRegionMvcData = GetRegionMvcData(fullyLoadedCp);
                    RegionModel region;
                    if (regions.TryGetValue(cpRegionMvcData.ViewName, out region))
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

                    EntityModel entity;
                    try
                    {
                        entity = ModelBuilderPipeline.CreateEntityModel(fullyLoadedCp, localization);
                    }
                    catch (Exception ex)
                    {
                        //if there is a problem mapping the item, we replace it with an exception entity
                        //and carry on processing - this should not cause a failure in the rendering of
                        //the page as a whole
                        Log.Error(ex);
                        entity = new ExceptionEntity(ex)
                        {
                            MvcData = GetMvcData(fullyLoadedCp) // TODO: The regular View won't expect an ExceptionEntity model. Should use an Exception View (?)
                        };
                    }

                    if (conditionalEntityEvaluator == null || conditionalEntityEvaluator.IncludeEntity(entity))
                    {
                        region.Entities.Add(entity);
                    }
                }

                // Create Regions from Include Pages
                if (includes != null)
                {
                    foreach (IPage includePage in includes)
                    {
                        PageModel includePageModel = ModelBuilderPipeline.CreatePageModel(includePage, null, localization);

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

                entityModel.XpmMetadata.Add("ComponentTemplateID", cp.ComponentTemplate.Id);
                entityModel.XpmMetadata.Add("ComponentTemplateModified", cp.ComponentTemplate.RevisionDate.ToString("s"));
                entityModel.XpmMetadata.Add("IsRepositoryPublished", cp.IsDynamic ? "1" : "0");
                entityModel.MvcData = mvcData;

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
                Type modelType = GetModelTypeFromSemanticMapping(semanticSchema, baseModelType);

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
                entityModel.XpmMetadata = GetXpmMetadata(component);

                if (entityModel is MediaItem && component.Multimedia != null && component.Multimedia.Url != null)
                {
                    MediaItem mediaItem = (MediaItem)entityModel;
                    mediaItem.Url = component.Multimedia.Url;
                    mediaItem.FileName = component.Multimedia.FileName;
                    mediaItem.FileSize = component.Multimedia.Size;
                    mediaItem.MimeType = component.Multimedia.MimeType;
                }

                if (entityModel is Link)
                {
                    Link link = (Link)entityModel;
                    if (String.IsNullOrEmpty(link.Url))
                    {
                        link.Url = SiteConfiguration.LinkResolver.ResolveLink(component.Id);
                    }
                }
            }
        }
        #endregion

        private static IComponentPresentation LoadDcp(string componentUri, string templateUri)
        {
            // TODO: should this not be in a Provider?
            using (new Tracer(componentUri, templateUri))
            {
                ComponentFactory componentFactory = new ComponentFactory();
                IComponent component;
                if (componentFactory.TryGetComponent(componentUri, out component, templateUri))
                {
                    //var componentTcmUri = new TcmUri(componentUri);
                    var templateTcmUri = new TcmUri(templateUri);

                    var publicationCriteria = new PublicationCriteria(templateTcmUri.PublicationId);
                    var itemReferenceCriteria = new ItemReferenceCriteria(templateTcmUri.ItemId);
                    var itemTypeTypeCriteria = new ItemTypeCriteria(32);

                    var query = new global::Tridion.ContentDelivery.DynamicContent.Query.Query(
                        CriteriaFactory.And(new Criteria[] { publicationCriteria, itemReferenceCriteria, itemTypeTypeCriteria }));

                    var results = query.ExecuteEntityQuery();
                    if (results != null)
                    {
                        var componentPresentation = new ComponentPresentation
                        {
                            Component = component as Component,
                            IsDynamic = true
                        };

                        var templateMeta = (ITemplateMeta)results.FirstOrDefault();
                        var template = new ComponentTemplate
                        {
                            Id = templateUri,
                            Title = templateMeta.Title,
                            OutputFormat = templateMeta.OutputFormat
                        };

                        componentPresentation.ComponentTemplate = template;
                        return componentPresentation;
                    }
                }

                throw new DxaItemNotFoundException(GetDxaIdentifierFromTcmUri(componentUri, templateUri));
            }
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

            return pageModel;
        }


        protected virtual ViewModel CreateViewModel(MappingData mappingData)
        {
            Type modelType = mappingData.TargetType; // TODO: why is this not a separate parameter?


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
            foreach (PropertyInfo pi in modelType.GetProperties())
            {
                bool multival = pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
                Type propertyType = multival ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType;
                if (propertySemantics.ContainsKey(pi.Name))
                {
                    foreach (SemanticProperty info in propertySemantics[pi.Name])
                    {
                        IField field = GetFieldFromSemantics(mappingData, info);
                        if (field != null && (field.Values.Count > 0 || field.EmbeddedValues.Count > 0))
                        {
                            pi.SetValue(model, MapFieldValues(field, propertyType, multival, mappingData));
                            xpmPropertyMetadata.Add(pi.Name, GetFieldXPath(field));
                            break;
                        }

                        // Special mapping cases require SourceEntity to be set
                        if (mappingData.SourceEntity == null)
                        {
                            continue;
                        }

                        bool processed = false;
                        if (info.PropertyName == "_self")
                        {
                            //Map the whole entity to an image property, or a resolved link to the entity to a Url field
                            if (typeof(MediaItem).IsAssignableFrom(propertyType) || typeof(Link).IsAssignableFrom(propertyType) || propertyType == typeof(String))
                            {
                                object mappedSelf = MapComponent(mappingData.SourceEntity, propertyType, mappingData.Localization);
                                if (multival)
                                {
                                    IList genericList = CreateGenericList(propertyType);
                                    genericList.Add(mappedSelf);
                                    pi.SetValue(model, genericList);
                                }
                                else
                                {
                                    pi.SetValue(model, mappedSelf);
                                }
                                processed = true;
                            }
                        }
                        else if (info.PropertyName == "_all" && pi.PropertyType == typeof(Dictionary<string, string>))
                        {
                            //Map all fields into a single (Dictionary) property
                            pi.SetValue(model, GetAllFieldsAsDictionary(mappingData.SourceEntity));
                            processed = true;
                        }

                        if (processed)
                        {
                            break;
                        }
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

        private static IField GetFieldFromSemantics(MappingData mapData, SemanticProperty info)
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
                        SemanticSchemaField matchingField = mapData.SemanticSchema.FindFieldBySemantics(fieldSemantics);
                        if (matchingField != null)
                        {
                            return ExtractMatchedField(matchingField, (matchingField.IsMetadata && mapData.Meta!=null) ? mapData.Meta : mapData.Content, mapData.EmbedLevel);
                        }
                    }
                }
            }
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


        private static IField ExtractMatchedField(SemanticSchemaField matchingField, IFieldSet fields, int embedLevel, string path = null)
        {
            if (path==null)
            {
                path = matchingField.Path;
                while (embedLevel >= -1 && path.Contains("/"))
                {
                    int pos = path.IndexOf("/", StringComparison.Ordinal);
                    path = path.Substring(pos+1);
                    embedLevel--;
                }
            }
            string[] bits = path.Split('/');
            if (fields.ContainsKey(bits[0]))
            {
                if (bits.Length > 1)
                {
                    int pos = path.IndexOf("/", StringComparison.Ordinal);
                    return ExtractMatchedField(matchingField, fields[bits[0]].EmbeddedValues[0], embedLevel, path.Substring(pos + 1));
                }

                return fields[bits[0]];
            }
            return null;
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


        private object MapFieldValues(IField field, Type modelType, bool multival, MappingData mapData)
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
                            mappedValues.Add(MapEmbeddedFields(value, modelType, mapData));
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
                            RichText richText = richTextProcessor.ProcessRichText(value);
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

        private static string GetFieldXPath(IField field)
        {
            return field.XPath;
        }

        protected virtual IDictionary<string, string> GetXpmMetadata(IComponent comp)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            if (comp != null)
            {
                result.Add("ComponentID", comp.Id);
                result.Add("ComponentModified", comp.RevisionDate.ToString("s"));
            }
            return result;
        }

        protected virtual IDictionary<string, string> GetXpmMetadata(IPage page)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            if (page != null)
            {
                result.Add("PageID", page.Id);
                result.Add("PageModified", page.RevisionDate.ToString("s"));
                result.Add("PageTemplateID", page.PageTemplate.Id);
                result.Add("PageTemplateModified", page.PageTemplate.RevisionDate.ToString("s"));
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

        private ViewModel MapEmbeddedFields(IFieldSet embeddedFields, Type modelType, MappingData mapData)
        {
            MappingData embeddedMappingData = new MappingData
                {
                    TargetType = modelType,
                    Content = embeddedFields,
                    Meta = null,
                    EntityNames = mapData.EntityNames, // TODO: should this not be re-determined for the embedded model type?
                    ParentDefaultPrefix = mapData.ParentDefaultPrefix,
                    TargetEntitiesByPrefix = mapData.TargetEntitiesByPrefix, // TODO: should this not be re-determined for the embedded model type?
                    SemanticSchema = mapData.SemanticSchema, // TODO: should this not be re-determined for the embedded model type?
                    EmbedLevel = mapData.EmbedLevel + 1,
                    Localization = mapData.Localization
                };

            return CreateViewModel(embeddedMappingData);
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
                        string val = component.Fields[fieldname].Value;
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
                    string val = component.MetadataFields[fieldname].Value;
                    if (val != null)
                    {
                        values.Add(fieldname, val);
                    }
                }
            }
            return values;
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
            string titlePostfix = GetResource("core.pageTitleSeparator") + GetResource("core.pageTitlePostfix");
            //if we still dont have a title, use the page title
            if (title == null)
            {
                title = Regex.Replace(page.Title, @"^\d{3}\s", String.Empty);
                // Index and Default are not a proper titles for an HTML page
                if (title.ToLowerInvariant().Equals("index") || title.ToLowerInvariant().Equals("default"))
                {
                    title = GetResource("core.defaultPageTitle");
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
            // TODO: meta.Add("fb:admins", Configuration.GetConfig("core.fbadmins");
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
                
        private string GetResource(string name)
        {
            if (_resourceProvider == null)
            {
                _resourceProvider = new ResourceProvider();
            }
            return _resourceProvider.GetObject(name, CultureInfo.CurrentUICulture).ToString();
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
            string regionName = null;
            string module = SiteConfiguration.GetDefaultModuleName(); //Default module
            if (cp.ComponentTemplate.MetadataFields != null)
            {
                if (cp.ComponentTemplate.MetadataFields.ContainsKey("regionView"))
                {
                    // split region view on colon, use first part as area (module) name
                    string[] regionViewParts = cp.ComponentTemplate.MetadataFields["regionView"].Value.Split(':');
                    if (regionViewParts.Length > 1)
                    {
                        module = regionViewParts[0].Trim();
                        regionName = regionViewParts[1].Trim();
                    }
                    else
                    {
                        regionName = regionViewParts[0].Trim();
                    }
                }
            }

            // fallback if no meta - use the CT title
            if (regionName == null)
            {
                Match match = Regex.Match(cp.ComponentTemplate.Title, @".*?\[(.*?)\]");
                if (match.Success)
                {
                    regionName = match.Groups[1].Value;

                    // split region view on colon, use first part as area (module) name
                    string[] regionViewParts = regionName.Split(':');
                    if (regionViewParts.Length > 1)
                    {
                        module = regionViewParts[0].Trim();
                        regionName = regionViewParts[1].Trim();
                    }
                }
            }

            regionName = regionName ?? "Main"; // default region name

            MvcData regionMvcData = new MvcData { AreaName = module, ViewName = regionName };
            InitializeRegionMvcData(regionMvcData);
            return regionMvcData;
        }

        private static RegionModel GetRegionFromIncludePage(IPage page)
        {
            string regionName = page.Title;

            MvcData regionMvcData = new MvcData(regionName);
            InitializeRegionMvcData(regionMvcData);

            return new RegionModel(regionName)
            {
                MvcData = regionMvcData,
                XpmMetadata = new Dictionary<string, string>
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
            Type regionModelType = ModelTypeRegistry.GetViewModelType(regionMvcData);
            RegionModel regionModel = (RegionModel) Activator.CreateInstance(regionModelType, regionMvcData.ViewName);
            regionModel.MvcData = regionMvcData;
            return regionModel;
        }

        /// <summary>
        /// Creates predefined Regions from Page Template metadata.
        /// </summary>
        private static void CreatePredefinedRegions(RegionModelSet regions, IPageTemplate pageTemplate)
        {
            IFieldSet ptMetadataFields = pageTemplate.MetadataFields;
            IField regionsField;
            if (ptMetadataFields == null || !ptMetadataFields.TryGetValue("regions", out regionsField)) // TODO: "region" instead of "regions"
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
            IPageTemplate template = page.PageTemplate;
            string viewName = template.Title.RemoveSpaces();

            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("view"))
                {
                    viewName = template.MetadataFields["view"].Value;
                }
            }

            MvcData mvcData = CreateViewData(viewName);
            // defaults
            mvcData.ControllerName = SiteConfiguration.GetPageController();
            mvcData.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
            mvcData.ActionName = SiteConfiguration.GetPageAction();

            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("htmlId"))
                {
                    // strip illegal characters to ensure valid html in the view
                    mvcData.HtmlId = template.MetadataFields["htmlId"].Value.StripIllegalCharacters(@"[^\w\-]");
                }
                if (template.MetadataFields.ContainsKey("htmlClasses"))
                {
                    // strip illegal characters to ensure valid html in the view (allow spaces for multiple classes)
                    mvcData.HtmlClasses = template.MetadataFields["htmlClasses"].Value.StripIllegalCharacters(@"[^\w\-\ ]");
                }
            }

            return mvcData;
        }

        /// <summary>
        /// Determine MVC data such as view, controller and area name from a Component Presentation
        /// </summary>
        /// <param name="cp">The DD4T Component Presentation</param>
        /// <returns>MVC data</returns>
        private static MvcData GetMvcData(IComponentPresentation cp)
        {
            IComponentTemplate template = cp.ComponentTemplate;
            string viewName = Regex.Replace(template.Title, @"\[.*\]|\s", String.Empty);

            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("view"))
                {
                    viewName = template.MetadataFields["view"].Value;
                }
            }

            MvcData mvcData = CreateViewData(viewName);
            // defaults
            mvcData.ControllerName = SiteConfiguration.GetEntityController();
            mvcData.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
            mvcData.ActionName = SiteConfiguration.GetEntityAction();
            mvcData.RouteValues = new Dictionary<string, string>();

            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("controller"))
                {
                    string[] controllerNameParts = template.MetadataFields["controller"].Value.Split(':');
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
                if (template.MetadataFields.ContainsKey("regionView"))
                {
                    string[] regionNameParts = template.MetadataFields["regionView"].Value.Split(':');
                    if (regionNameParts.Length > 1)
                    {
                        mvcData.RegionName = regionNameParts[1];
                        mvcData.RegionAreaName = regionNameParts[0];
                    }
                    else
                    {
                        mvcData.RegionName = regionNameParts[0];
                        mvcData.RegionAreaName = SiteConfiguration.GetDefaultModuleName();
                    }
                }
                if (template.MetadataFields.ContainsKey("action"))
                {
                    mvcData.ActionName = template.MetadataFields["action"].Value;
                }
                if (template.MetadataFields.ContainsKey("routeValues"))
                {
                    string[] routeValues = template.MetadataFields["routeValues"].Value.Split(',');
                    foreach (string routeValue in routeValues)
                    {
                        string[] routeValueParts = routeValue.Trim().Split(':');
                        if (routeValueParts.Length > 1 && !mvcData.RouteValues.ContainsKey(routeValueParts[0]))
                        {
                            mvcData.RouteValues.Add(routeValueParts[0], routeValueParts[1]);
                        }
                    }
                }
                if (template.MetadataFields.ContainsKey("htmlId"))
                {
                    // strip illegal characters to ensure valid html in the view
                    mvcData.HtmlId = template.MetadataFields["htmlId"].Value.StripIllegalCharacters(@"[^\w\-]");
                }
                if (template.MetadataFields.ContainsKey("htmlClasses"))
                {
                    // strip illegal characters to ensure valid html in the view (allow spaces for multiple classes)
                    mvcData.HtmlClasses = template.MetadataFields["htmlClasses"].Value.StripIllegalCharacters(@"[^\w\-\ ]");
                }
            }

            return mvcData;
        }


        private static MvcData CreateViewData(string viewName)
        {
            string[] nameParts = viewName.Split(':');
            string areaName;
            if (nameParts.Length > 1)
            {
                areaName = nameParts[0].Trim();
                viewName = nameParts[1].Trim();
            }
            else
            {
                areaName = SiteConfiguration.GetDefaultModuleName();
                viewName = nameParts[0].Trim();
            }
            return new MvcData { ViewName = viewName, AreaName = areaName };
        }
    }
}
