using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Tridion.ContentManager;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Default Page and Entity Model Builder implementation (based on DXA R2 Data Model).
    /// </summary>
    public class DefaultModelBuilderR2 : IPageModelBuilder, IEntityModelBuilder
    {
        private static readonly Regex _tcmUriRegex = new Regex(@"tcm:\d+-\d+(-\d+)?", RegexOptions.Compiled);
        private static readonly Regex _compLinkStartTagRegex = new Regex(@"(?<before><a\s.*href="")(?<tcmUri>tcm:\d+-\d+)(?<after>""[^>]*>)", RegexOptions.Compiled);
        private static readonly Regex _compLinkEndTagRegex = new Regex(@"(?<before></a><!--CompLink\s)(?<tcmUri>tcm:\d+-\d+)(?<after>-->)", RegexOptions.Compiled);

        private class MappingData
        {
            public string ModelId { get; set; }
            public Type ModelType { get; set; }
            public SemanticSchema SemanticSchema { get; set; }
            public SemanticSchemaField EmbeddedSemanticSchemaField { get; set; }
            public int EmbedLevel { get; set; }
            public ViewModelData SourceViewModel { get; set; }
            public ContentModelData Fields { get; set; }
            public ContentModelData MetadataFields { get; set; }
            public string ContextXPath { get; set; }
            public Localization Localization { get; set; }
        }

        /// <summary>
        /// Builds a strongly typed Page Model from a given DXA R2 Data Model.
        /// </summary>
        /// <param name="pageModel">The strongly typed Page Model to build. Is <c>null</c> for the first Page Model Builder in the pipeline.</param>
        /// <param name="pageModelData">The DXA R2 Data Model.</param>
        /// <param name="includePageRegions">Indicates whether Include Page Regions should be included.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        public void BuildPageModel(ref PageModel pageModel, PageModelData pageModelData, bool includePageRegions, Localization localization)
        {
            using (new Tracer(pageModel, pageModelData, includePageRegions, localization))
            {
                Common.Models.MvcData mvcData = CreateMvcData(pageModelData.MvcData, "PageModel");

                if (pageModelData.SchemaId == null)
                {
                    pageModel = new PageModel(pageModelData.Id)
                    {
                        ExtensionData = pageModelData.ExtensionData,
                        HtmlClasses = pageModelData.HtmlClasses,
                        XpmMetadata = pageModelData.XpmMetadata,
                    };
                }
                else
                {
                    MappingData mappingData = new MappingData
                    {
                        SourceViewModel = pageModelData,
                        ModelId = pageModelData.Id,
                        ModelType = ModelTypeRegistry.GetViewModelType(mvcData),
                        SemanticSchema = SemanticMapping.GetSchema(pageModelData.SchemaId, localization),
                        MetadataFields = pageModelData.Metadata,
                        Localization = localization
                    };
                    pageModel = (PageModel) CreateViewModel(mappingData);
                }

                pageModel.MvcData = mvcData;
                pageModel.Meta = ResolveMetaLinks(pageModelData.Meta); // TODO TSI-1267: Link Resolving should eventually be done in Model Service. 
                pageModel.Title = PostProcessPageTitle(pageModelData.Title, localization); // TODO TSI-24: This should eventually be done in Model Service.

                if (pageModelData.Regions != null)
                {
                    IEnumerable<RegionModelData> regions = includePageRegions ? pageModelData.Regions : pageModelData.Regions.Where(r => r.IncludePageUrl == null);
                    pageModel.Regions.UnionWith(regions.Select(data => CreateRegionModel(data, localization)));
                }
            }
        }

        /// <summary>
        /// Builds a strongly typed Entity Model based on a given DXA R2 Data Model.
        /// </summary>
        /// <param name="entityModel">The strongly typed Entity Model to build. Is <c>null</c> for the first Entity Model Builder in the pipeline.</param>
        /// <param name="entityModelData">The DXA R2 Data Model.</param>
        /// <param name="baseModelType">The base type for the Entity Model to build.</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        public void BuildEntityModel(ref EntityModel entityModel, EntityModelData entityModelData, Type baseModelType, Localization localization)
        {
            using (new Tracer(entityModel, entityModelData, baseModelType, localization))
            {
                Common.Models.MvcData mvcData = CreateMvcData(entityModelData.MvcData, "EntityModel");
                SemanticSchema semanticSchema = SemanticMapping.GetSchema(entityModelData.SchemaId, localization);

                Type modelType = (baseModelType == null) ?
                    ModelTypeRegistry.GetViewModelType(mvcData) :
                    semanticSchema.GetModelTypeFromSemanticMapping(baseModelType);

                MappingData mappingData = new MappingData
                {
                    SourceViewModel = entityModelData,
                    ModelType = modelType,
                    SemanticSchema = semanticSchema,
                    Fields = entityModelData.Content,
                    MetadataFields = entityModelData.Metadata,
                    Localization = localization
                };

                entityModel = (EntityModel) CreateViewModel(mappingData);

                entityModel.Id = entityModelData.Id;
                entityModel.MvcData = mvcData ?? entityModel.GetDefaultView(localization);
            }
        }

        private static ViewModel CreateViewModel(MappingData mappingData)
        {
            ViewModelData viewModelData = mappingData.SourceViewModel;
            EntityModelData entityModelData = viewModelData as EntityModelData;

            ViewModel result;
            if (string.IsNullOrEmpty(mappingData.ModelId))
            {
                // Use parameterless constructor
                result = (ViewModel) Activator.CreateInstance(mappingData.ModelType);
            }
            else
            {
                // Pass model Identifier in constructor.
                result = (ViewModel) Activator.CreateInstance(mappingData.ModelType, mappingData.ModelId);
            }

            result.ExtensionData = viewModelData.ExtensionData;
            result.HtmlClasses = viewModelData.HtmlClasses;
            result.XpmMetadata = viewModelData.XpmMetadata;

            MediaItem mediaItem = result as MediaItem;
            if (mediaItem != null)
            {
                BinaryContentData binaryContent = entityModelData?.BinaryContent;
                if (binaryContent == null)
                {
                    throw new DxaException(
                        $"Unable to create Media Item ('{mappingData.ModelType.Name}') because the Data Model '{entityModelData?.Id}' does not contain Binary Content Data."
                        );
                }
                mediaItem.Url = binaryContent.Url;
                mediaItem.FileName = binaryContent.FileName;
                mediaItem.MimeType = binaryContent.MimeType;
                mediaItem.FileSize = binaryContent.FileSize;
            }

            EclItem eclItem = result as EclItem;
            if (eclItem != null)
            {
                ExternalContentData externalContent = entityModelData.ExternalContent;
                if (externalContent == null)
                {
                    throw new DxaException(
                        $"Unable to create ECL Item ('{mappingData.ModelType.Name}') because the Data Model '{entityModelData.Id}' does not contain External Content Data."
                        );
                }
                eclItem.EclDisplayTypeId = externalContent.DisplayTypeId;
                eclItem.EclExternalMetadata = externalContent.Metadata;
                eclItem.EclUri = externalContent.Id;
            }

            MapSemanticProperties(result, mappingData);

            return result;
        }

        private static void MapSemanticProperties(ViewModel viewModel, MappingData mappingData)
        {
            Type modelType = viewModel.GetType();
            IDictionary<string, List<SemanticProperty>> propertySemanticsMap = ModelTypeRegistry.GetPropertySemantics(modelType);
            IDictionary<string, string> xpmPropertyMetadata = new Dictionary<string, string>();

            foreach (KeyValuePair<string, List<SemanticProperty>> propertySemantics in propertySemanticsMap)
            {
                PropertyInfo modelProperty = modelType.GetProperty(propertySemantics.Key);
                List<SemanticProperty> semanticProperties = propertySemantics.Value;

                bool isFieldMapped = false;
                string fieldXPath = null;
                foreach (SemanticProperty semanticProperty in semanticProperties)
                {
                    if (semanticProperty.PropertyName == SemanticProperty.AllFields)
                    {
                        modelProperty.SetValue(viewModel, GetAllFieldsAsDictionary(mappingData));
                        isFieldMapped = true;
                        break;
                    }
                    if ((semanticProperty.PropertyName == SemanticProperty.Self) && mappingData.SemanticSchema.HasSemanticType(semanticProperty.SemanticType))
                    {
                        try
                        {
                            object mappedSelf = MapComponentLink((EntityModelData) mappingData.SourceViewModel, modelProperty.PropertyType, mappingData.Localization);
                            modelProperty.SetValue(viewModel, mappedSelf);
                            isFieldMapped = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Debug($"Self mapping failed for {modelType.Name}.{modelProperty.Name}: {ex.Message}");
                            continue;
                        }
                    }

                    FieldSemantics fieldSemantics = new FieldSemantics(
                        semanticProperty.SemanticType.Vocab,
                        semanticProperty.SemanticType.EntityName,
                        semanticProperty.PropertyName,
                        null);
                    SemanticSchemaField semanticSchemaField = (mappingData.EmbeddedSemanticSchemaField == null) ?
                        mappingData.SemanticSchema.FindFieldBySemantics(fieldSemantics) :
                        mappingData.EmbeddedSemanticSchemaField.FindFieldBySemantics(fieldSemantics);
                    if (semanticSchemaField == null)
                    {
                        // No matching Semantic Schema Field found for this Semantic Property; maybe another one will match.
                        continue;
                    }

                    // Matching Semantic Schema Field found
                    fieldXPath = semanticSchemaField.GetXPath(mappingData.ContextXPath);
                    ContentModelData fields = semanticSchemaField.IsMetadata ? mappingData.MetadataFields : mappingData.Fields;
                    object fieldValue = FindFieldValue(semanticSchemaField, fields, mappingData.EmbedLevel);
                    if (fieldValue == null)
                    {
                        // No field value found; maybe we will find a value for another Semantic Property.
                        continue;
                    }

                    try
                    {
                        object mappedPropertyValue = MapField(fieldValue, modelProperty.PropertyType, semanticSchemaField, mappingData);
                        modelProperty.SetValue(viewModel, mappedPropertyValue);
                    }
                    catch (Exception ex)
                    {
                        throw new DxaException(
                            $"Unable to map field '{semanticSchemaField.Name}' to property {modelType.Name}.{modelProperty.Name} of type '{modelProperty.PropertyType.FullName}'.",
                            ex);
                    }
                    isFieldMapped = true;
                    break;
                }

                if (fieldXPath != null)
                {
                    xpmPropertyMetadata.Add(modelProperty.Name, fieldXPath);
                }
                else if (!isFieldMapped)
                {
                    string formattedSemanticProperties = string.Join(", ", semanticProperties.Select(sp => sp.ToString()));
                    Log.Debug(
                        $"Property {modelType.Name}.{modelProperty.Name} cannot be mapped to a CM field of {mappingData.SemanticSchema}. Semantic properties: {formattedSemanticProperties}.");
                }
            }

            EntityModel entityModel = viewModel as EntityModel;
            if ((entityModel != null) && mappingData.Localization.IsStaging)
            {
                entityModel.XpmPropertyMetadata = xpmPropertyMetadata;
            }
        }

        private static object FindFieldValue(SemanticSchemaField semanticSchemaField, ContentModelData fields, int embedLevel)
        {
            string[] pathSegments = semanticSchemaField.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            object fieldValue = null;
            foreach (string pathSegment in pathSegments.Skip(embedLevel + 1))
            {
                if ((fields == null) || !fields.TryGetValue(pathSegment, out fieldValue))
                {
                    return null;
                }
                if (fieldValue is ContentModelData[])
                {
                    fields = ((ContentModelData[]) fieldValue)[0];
                }
                else
                {
                    fields = fieldValue as ContentModelData;
                }
            }

            return fieldValue;
        }

        private static object MapField(object fieldValues, Type modelPropertyType, SemanticSchemaField semanticSchemaField, MappingData mappingData)
        {
            Type sourceType = fieldValues.GetType();
            bool isArray = sourceType.IsArray;
            if (isArray)
            {
                sourceType = sourceType.GetElementType();
            }

            bool isListProperty = modelPropertyType.IsGenericType && (modelPropertyType.GetGenericTypeDefinition() == typeof(List<>));
            Type targetType = isListProperty ? modelPropertyType.GetGenericArguments()[0] : modelPropertyType;

            // Convert.ChangeType cannot convert non-nullable types to nullable types, so don't try that.
            Type bareTargetType = targetType;
            if (modelPropertyType.IsGenericType && modelPropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                bareTargetType = modelPropertyType.GenericTypeArguments[0];
            }

            IList mappedValues = CreateGenericList(targetType);

            switch (sourceType.Name)
            {
                case "String":
                    if (isArray)
                    {
                        foreach (string fieldValue in (string[]) fieldValues)
                        {
                            mappedValues.Add(MapString(fieldValue, bareTargetType));
                        }
                    }
                    else
                    {
                        mappedValues.Add(MapString((string) fieldValues, bareTargetType));
                    }
                    break;

                case "DateTime":
                    if (isArray)
                    {
                        foreach (DateTime fieldValue in (DateTime[]) fieldValues)
                        {
                            mappedValues.Add(Convert.ChangeType(fieldValue, bareTargetType));
                        }
                    }
                    else
                    {
                        mappedValues.Add(Convert.ChangeType(fieldValues, bareTargetType));
                    }
                    break;

                case "RichTextData":
                    if (isArray)
                    {
                        foreach (RichTextData fieldValue in (RichTextData[]) fieldValues)
                        {
                            mappedValues.Add(MapRichText(fieldValue, targetType, mappingData.Localization));
                        }
                    }
                    else
                    {
                        mappedValues.Add(MapRichText((RichTextData) fieldValues, targetType, mappingData.Localization));
                    }
                    break;

                case "ContentModelData":
                    string fieldXPath = semanticSchemaField.GetXPath(mappingData.ContextXPath);
                    if (isArray)
                    {
                        int index = 1;
                        foreach (ContentModelData embeddedFields in (ContentModelData[]) fieldValues)
                        {
                            string indexedFieldXPath = $"{fieldXPath}[{index++}]";
                            mappedValues.Add(MapEmbeddedFields(embeddedFields, targetType, semanticSchemaField, indexedFieldXPath, mappingData));
                        }
                    }
                    else
                    {
                        string indexedFieldXPath = $"{fieldXPath}[1]";
                        mappedValues.Add(MapEmbeddedFields((ContentModelData) fieldValues, targetType, semanticSchemaField, indexedFieldXPath, mappingData));
                    }
                    break;

                case "EntityModelData":
                    if (isArray)
                    {
                        foreach (EntityModelData entityModelData in (EntityModelData[]) fieldValues)
                        {
                            mappedValues.Add(MapComponentLink(entityModelData, targetType, mappingData.Localization));
                        }
                    }
                    else
                    {
                        mappedValues.Add(MapComponentLink((EntityModelData) fieldValues, targetType, mappingData.Localization));
                    }
                    break;

                case "KeywordModelData":
                    if (isArray)
                    {
                        foreach (KeywordModelData keywordModelData in (KeywordModelData[]) fieldValues)
                        {
                            mappedValues.Add(MapKeyword(keywordModelData, targetType, mappingData.Localization));
                        }
                    }
                    else
                    {
                        mappedValues.Add(MapKeyword((KeywordModelData) fieldValues, targetType, mappingData.Localization));
                    }
                    break;


                default:
                    throw new DxaException($"Unexpected field type: '{sourceType.Name}'.");
            }

            if (isListProperty)
            {
                return mappedValues;
            }

            return (mappedValues.Count == 0) ? null : mappedValues[0];
        }

        private static object MapString(string stringValue, Type targetType)
        {
            if (targetType == typeof(RichText))
            {
                return new RichText(stringValue);
            }
            return Convert.ChangeType(stringValue, targetType, CultureInfo.InvariantCulture.NumberFormat);
        }

        private static object MapComponentLink(EntityModelData entityModelData, Type targetType, Localization localization)
        {
            // TODO TSI-878: Use EntityModelData.LinkUrl (resolved by Model Service)
            if (targetType == typeof(Link))
            {
                return new Link { Url = ResolveLinkUrl(entityModelData, localization) };
            }

            if (targetType == typeof(string))
            {
                return ResolveLinkUrl(entityModelData, localization);
            }

            if (!typeof(EntityModel).IsAssignableFrom(targetType))
            {
                throw new DxaException($"Cannot map Component Link to property of type '{targetType.Name}'.");
            }

            return ModelBuilderPipelineR2.CreateEntityModel(entityModelData, targetType, localization);
        }

        private static object MapKeyword(KeywordModelData keywordModelData, Type targetType, Localization localization)
        {
            if (typeof(KeywordModel).IsAssignableFrom(targetType))
            {
                KeywordModel result;
                if (keywordModelData.SchemaId == null)
                {
                    result = new KeywordModel
                    {
                        ExtensionData = keywordModelData.ExtensionData
                    };
                }
                else
                {
                    MappingData keywordMappingData = new MappingData
                    {
                        SourceViewModel = keywordModelData,
                        ModelType = targetType,
                        SemanticSchema = SemanticMapping.GetSchema(keywordModelData.SchemaId, localization),
                        MetadataFields = keywordModelData.Metadata,
                        Localization = localization
                    };

                    result = (KeywordModel) CreateViewModel(keywordMappingData);
                }

                result.Id = keywordModelData.Id;
                result.Title = keywordModelData.Title;
                result.Description = keywordModelData.Description;
                result.Key = keywordModelData.Key;
                result.TaxonomyId = keywordModelData.TaxonomyId;

                return result;
            }

            if (targetType == typeof(Tag))
            {
                return new Tag
                {
                    DisplayText = GetKeywordDisplayText(keywordModelData),
                    Key = keywordModelData.Key,
                    TagCategory = localization.GetCmUri(keywordModelData.TaxonomyId, (int) ItemType.Category)
                };
            }

            if (targetType == typeof(bool))
            {
                string key = string.IsNullOrEmpty(keywordModelData.Key) ? keywordModelData.Title : keywordModelData.Key;
                return Convert.ToBoolean(key);
            }

            return GetKeywordDisplayText(keywordModelData);
        }

        private static string GetKeywordDisplayText(KeywordModelData keywordModelData)
            => string.IsNullOrEmpty(keywordModelData.Description) ? keywordModelData.Title : keywordModelData.Description;

        private static string ResolveLinkUrl(EntityModelData entityModelData, Localization localization)
        {
            string componentUri = $"tcm:{localization.Id}-{entityModelData.Id}";
            return SiteConfiguration.LinkResolver.ResolveLink(componentUri);
        }

        private static object MapRichText(RichTextData richTextData, Type targetType, Localization localization)
        {
            // TODO TSI-1267: Component Links resolving should be done in Model Service.
            ResolveRichTextLinks(richTextData);

            IList<IRichTextFragment> fragments = new List<IRichTextFragment>();
            foreach (object fragment in richTextData.Fragments)
            {
                string htmlFragment = fragment as string;
                if (htmlFragment == null)
                {
                    // Embedded Entity Model (for Media Items)
                    MediaItem mediaItem = (MediaItem) ModelBuilderPipelineR2.CreateEntityModel((EntityModelData) fragment, typeof(MediaItem), localization);
                    mediaItem.IsEmbedded = true;
                    if (mediaItem.MvcData == null)
                    {
                        mediaItem.MvcData = mediaItem.GetDefaultView(localization);
                    }
                    fragments.Add(mediaItem);
                }
                else
                {
                    // HTML fragment.
                    fragments.Add(new RichTextFragment(htmlFragment));
                }
            }
            RichText richText = new RichText(fragments);

            if (targetType == typeof(RichText))
            {
                return richText;
            }

            return richText.ToString();
        }

        private static void ResolveRichTextLinks(RichTextData richTextData)
        {
            // 1st pass: resolve Component Links and suppress hyperlink start tags if needed.
            ILinkResolver linkResolver = SiteConfiguration.LinkResolver;
            List<string> suppressCompLinks = new List<string>();
            for (int i = 0; i < richTextData.Fragments.Count; i++)
            {
                string htmlFragment = richTextData.Fragments[i] as string;
                if (htmlFragment == null)
                {
                    // This is not an HTML fragment, but an embedded Entity Model.
                    continue;
                }

                string resolvedHtmlFragment = _compLinkStartTagRegex.Replace(
                    htmlFragment,
                    match =>
                    {
                        string tcmUri = match.Groups["tcmUri"].Value;
                        string resolvedLink = linkResolver.ResolveLink(tcmUri, resolveToBinary: true);
                        if (!string.IsNullOrEmpty(resolvedLink))
                        {
                            return match.Groups["before"] + resolvedLink + match.Groups["after"];
                        }
                        Log.Warn($"Link to Component '{tcmUri}' did not resolve; suppressing hyperlink in rich text.");
                        suppressCompLinks.Add(tcmUri);
                        return string.Empty; // This suppresses the hyperlink start tag only; end tag will be done later.
                    }
                    );

                richTextData.Fragments[i] = resolvedHtmlFragment;
            }

            // 2nd pass: remove the CompLink markers and suppress hyperlink end tags if needed.
            for (int i = 0; i < richTextData.Fragments.Count; i++)
            {
                string htmlFragment = richTextData.Fragments[i] as string;
                if (htmlFragment == null)
                {
                    // This is not an HTML fragment, but an embedded Entity Model.
                    continue;
                }

                string resolvedHtmlFragment = _compLinkEndTagRegex.Replace(
                    htmlFragment,
                    match =>
                    {
                        string tcmUri = match.Groups["tcmUri"].Value;
                        return suppressCompLinks.Contains(tcmUri) ? string.Empty : "</a>";
                    }
                    );

                richTextData.Fragments[i] = resolvedHtmlFragment;
            }
        }

        private static object MapEmbeddedFields(ContentModelData embeddedFields, Type targetType, SemanticSchemaField semanticSchemaField, string contextXPath, MappingData mappingData)
        {
            MappingData embeddedMappingData = new MappingData
            {
                ModelType = targetType,
                SemanticSchema = mappingData.SemanticSchema,
                EmbeddedSemanticSchemaField = semanticSchemaField,
                EmbedLevel = mappingData.EmbedLevel + 1,
                SourceViewModel = mappingData.SourceViewModel,
                Fields = embeddedFields,
                MetadataFields = embeddedFields,
                ContextXPath = contextXPath,
                Localization = mappingData.Localization
            };
            return CreateViewModel(embeddedMappingData);
        }

        private static IDictionary<string, string> GetAllFieldsAsDictionary(MappingData mappingData)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            if (mappingData.Fields != null)
            {
                foreach (KeyValuePair<string, object> field in mappingData.Fields)
                {
                    if ((field.Key == "settings"))
                    {
                        throw new NotImplementedException("'settings' field handling"); // TODO
                    }
                    result[field.Key] = GetFieldValuesAsStrings(field.Value, mappingData).FirstOrDefault();
                }
            }
            if (mappingData.MetadataFields != null)
            {
                foreach (KeyValuePair<string, object> field in mappingData.MetadataFields)
                {
                    result[field.Key] = GetFieldValuesAsStrings(field.Value, mappingData).FirstOrDefault();
                }
            }
            return result;
        }

        private static IEnumerable<string> GetFieldValuesAsStrings(object fieldValues, MappingData mappingData)
            => (IEnumerable<string>) MapField(fieldValues, typeof(List<string>), null, mappingData);

        private static IList CreateGenericList(Type listItemType)
        {
            ConstructorInfo genericListConstructor = typeof(List<>).MakeGenericType(listItemType).GetConstructor(Type.EmptyTypes);
            if (genericListConstructor == null)
            {
                // This should never happen.
                throw new DxaException($"Unable get constructor for generic list of '{listItemType.FullName}'.");
            }

            return (IList) genericListConstructor.Invoke(null);
        }


        private static IDictionary<string, string> ResolveMetaLinks(IDictionary<string, string> meta)
        {
            if (meta == null)
            {
                return null;
            }

            ILinkResolver linkResolver = SiteConfiguration.LinkResolver;

            Dictionary<string, string> result = new Dictionary<string, string>(meta.Count);
            foreach (KeyValuePair<string, string> kvp in meta)
            {
                string resolvedValue = _tcmUriRegex.Replace(kvp.Value, match => linkResolver.ResolveLink(match.Value, resolveToBinary: true));
                result.Add(kvp.Key, resolvedValue);
            }

            return result;
        }

        private static Common.Models.MvcData CreateMvcData(DataModel.MvcData data, string baseModelTypeName)
        {
            if (data == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(data.ViewName))
            {
                throw new DxaException("No View Name specified in MVC Data.");
            }

            string defaultControllerName;
            string defaultActionName;

            switch (baseModelTypeName)
            {
                case "PageModel":
                    defaultControllerName = "Page";
                    defaultActionName = "Page";
                    break;
                case "RegionModel":
                    defaultControllerName = "Region";
                    defaultActionName = "Region";
                    break;
                case "EntityModel":
                    defaultControllerName = "Entity";
                    defaultActionName = "Entity";
                    break;
                default:
                    throw new DxaException($"Unexpected baseModelTypeName '{baseModelTypeName}'");
            }

            return new Common.Models.MvcData
            {
                ControllerName = data.ControllerName ?? defaultControllerName,
                ControllerAreaName = data.ControllerAreaName ?? SiteConfiguration.GetDefaultModuleName(),
                ActionName = data.ActionName ?? defaultActionName,
                ViewName = data.ViewName,
                AreaName = data.AreaName ?? SiteConfiguration.GetDefaultModuleName(),
                RouteValues = data.Parameters
            };
        }

        private static RegionModel CreateRegionModel(RegionModelData regionModelData, Localization localization)
        {
            Common.Models.MvcData mvcData = CreateMvcData(regionModelData.MvcData, "RegionModel");
            Type regionModelType = ModelTypeRegistry.GetViewModelType(mvcData);

            RegionModel result = (RegionModel) Activator.CreateInstance(regionModelType, regionModelData.Name);

            result.ExtensionData = regionModelData.ExtensionData;
            result.HtmlClasses = regionModelData.HtmlClasses;
            result.MvcData = mvcData;
            result.XpmMetadata = regionModelData.XpmMetadata;

            if (regionModelData.Regions != null)
            {
                IEnumerable<RegionModel> nestedRegionModels = regionModelData.Regions.Select(data => CreateRegionModel(data, localization));
                result.Regions.UnionWith(nestedRegionModels);
            }

            if (regionModelData.Entities != null)
            {
                foreach (EntityModelData entityModelData in regionModelData.Entities)
                {
                    EntityModel entityModel;
                    try
                    {
                        entityModel = ModelBuilderPipelineR2.CreateEntityModel(entityModelData, null, localization);
                        entityModel.MvcData.RegionName = regionModelData.Name;
                    }
                    catch (Exception ex)
                    {
                        // If there is a problem mapping an Entity, we replace it with an ExceptionEntity which holds the error details and carry on.
                        Log.Error(ex);
                        entityModel = new ExceptionEntity(ex);
                    }
                    result.Entities.Add(entityModel);
                }
            }

            return result;
        }

        private static string PostProcessPageTitle(string pageTitle, Localization localization)
        {
            IDictionary coreResources = localization.GetResources("core");
            string titlePostfix = coreResources["core.pageTitleSeparator"].ToString() + coreResources["core.pageTitlePostfix"].ToString();
            return pageTitle + titlePostfix;
        }
    }
}
