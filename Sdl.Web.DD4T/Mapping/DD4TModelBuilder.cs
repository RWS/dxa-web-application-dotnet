using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using Newtonsoft.Json.Linq;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Models.Common;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Tridion.Config;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Model Builder implementation for DD4T - this contains the logic for mapping DD4T objects to View Models
    /// </summary>
    public class DD4TModelBuilder : BaseModelBuilder, IModelBuilder
    {
        // TODO: while it works perfectly well, this class is in need of some refactoring to make its behaviour a bit more understandable and maintainable,
        // as its currently very easy to get lost in the semantic mapping logic
        private ResourceProvider _resourceProvider;

        #region IModelBuilder members

        public virtual PageModel CreatePageModel(IPage page, IEnumerable<IPage> includes)
        {
            MvcData mvcData = GetMvcData(page);

            // TODO: Derive Page Model type from MVC data
            PageModel model = new PageModel(GetDxaIdentifierFromTcmUri(page.Id))
            {
                Title = page.Title, //default title - will be overridden later if appropriate
                MvcData = mvcData, 
                XpmMetadata = GetXpmMetadata(page)
            };

            IConditionalEntityEvaluator conditionalEntityEvaluator = SiteConfiguration.ConditionalEntityEvaluator;
            foreach (IComponentPresentation cp in page.ComponentPresentations)
            {
                RegionModel region = GetRegionFromComponentPresentation(cp);
                if (!model.Regions.ContainsKey(region.Name))
                {
                    model.Regions.Add(region);
                }

                EntityModel entity;
                try
                {
                    entity = CreateEntityModel(cp);
                }
                catch (Exception ex)
                {
                    //if there is a problem mapping the item, we replace it with an exception entity
                    //and carry on processing - this should not cause a failure in the rendering of
                    //the page as a whole
                    Log.Error(ex);
                    entity = new ExceptionEntity
                    {
                        Error = ex.Message,
                        MvcData = GetMvcData(cp)// TODO: The regular View won't expect an ExceptionEntity model. Should use an Exception View (?)
                    };
                }

                if (conditionalEntityEvaluator == null || conditionalEntityEvaluator.IncludeEntity(entity))
                {
                    model.Regions[region.Name].Entities.Add(entity);
                }
            }

            if (includes != null )
            {
                RegionModelSet regions = model.Regions;
                foreach (IPage includePage in includes)
                {
                    PageModel includePageModel = CreatePageModel(includePage, null);

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
                    model.Includes.Add(includePage.Title, includePageModel);
#pragma warning restore 618
                }

                if (mvcData.ViewName != "IncludePage")
                {
                    model.Title = ProcessPageMetadata(page, model.Meta);
                }
            }

            return model;
        }

        public virtual EntityModel CreateEntityModel(IComponentPresentation cp)
        {
            MvcData mvcData = GetMvcData(cp);
            Type modelType = ModelTypeRegistry.GetViewModelType(mvcData);

            EntityModel model = CreateEntityModel(cp.Component, modelType);
            model.XpmMetadata.Add("ComponentTemplateID", cp.ComponentTemplate.Id);
            model.XpmMetadata.Add("ComponentTemplateModified", cp.ComponentTemplate.RevisionDate.ToString("s"));
            model.MvcData = mvcData;
            return model;
        }

        public virtual EntityModel CreateEntityModel(IComponent component, Type type)
        {
            // get schema item id from tcmuri -> tcm:1-2-8
            string[] uriParts = component.Schema.Id.Split('-');
            long schemaId = Convert.ToInt64(uriParts[1]);
            MappingData mapData = new MappingData { SemanticSchema = SemanticMapping.GetSchema(schemaId.ToString(CultureInfo.InvariantCulture), WebRequestContext.Localization) };
            // get schema entity names (indexed by vocabulary)
            mapData.EntityNames = mapData.SemanticSchema != null ? mapData.SemanticSchema.GetEntityNames() : null;

            //TODO may need to merge with vocabs from embedded types
            mapData.TargetEntitiesByPrefix = GetEntityDataFromType(type);
            mapData.Content = component.Fields;
            mapData.Meta = component.MetadataFields;
            mapData.TargetType = type;
            mapData.SourceEntity = component;
            EntityModel model = CreateModelFromMapData(mapData);
            model.XpmMetadata = GetXpmMetadata(component);
            model.Id = GetDxaIdentifierFromTcmUri(component.Id);
            if (model is MediaItem && component.Multimedia != null && component.Multimedia.Url != null)
            {
                MediaItem mediaItem = (MediaItem)model;
                mediaItem.Url = component.Multimedia.Url;
                mediaItem.FileName = component.Multimedia.FileName;
                mediaItem.FileSize = component.Multimedia.Size;
                mediaItem.MimeType = component.Multimedia.MimeType;
            }
            return model;

        }
        #endregion

        protected virtual EntityModel CreateModelFromMapData(MappingData mapData)
        {
            EntityModel model = (EntityModel) Activator.CreateInstance(mapData.TargetType);
            Dictionary<string, string> xpmPropertyMetadata = new Dictionary<string, string>();
            Dictionary<string, List<SemanticProperty>> propertySemantics = FilterPropertySematicsByEntity(LoadPropertySemantics(mapData.TargetType),mapData);
            foreach (PropertyInfo pi in mapData.TargetType.GetProperties())
            {
                bool multival = pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
                Type propertyType = multival ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType;
                if (propertySemantics.ContainsKey(pi.Name))
                {
                    foreach (var info in propertySemantics[pi.Name])
                    {
                        IField field = GetFieldFromSemantics(mapData, info);
                        if (field != null && (field.Values.Count > 0 || field.EmbeddedValues.Count > 0))
                        {
                            pi.SetValue(model, GetFieldValues(field, propertyType, multival, mapData));
                            xpmPropertyMetadata.Add(pi.Name, GetFieldXPath(field));
                            break;
                        }
                        //Special cases
                        if (mapData.SourceEntity!=null)
                        {
                            bool processed = false;
                            //Map the whole entity to an image property, or a resolved link to the entity to a Url field
                            if (info.PropertyName == "_self")
                            {
                                if (propertyType == typeof(MediaItem) && mapData.SourceEntity.Multimedia != null)
                                {
                                     pi.SetValue(model, GetMultiMediaLinks(new List<IComponent> { mapData.SourceEntity }, propertyType, multival));
                                     processed = true;
                                }
                                else if (propertyType == typeof(Link) || propertyType == typeof(String))
                                {
                                    pi.SetValue(model, GetMultiComponentLinks(new List<IComponent> { mapData.SourceEntity }, propertyType, multival));
                                    processed = true;    
                                }
                            }
                            //Map all fields into a single (Dictionary) property
                            else if (info.PropertyName == "_all" && pi.PropertyType == typeof(Dictionary<string,string>))
                            {
                                pi.SetValue(model, GetAllFieldsAsDictionary(mapData.SourceEntity));
                                processed = true;
                            }

                            if (processed)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            model.XpmPropertyMetadata = xpmPropertyMetadata;

            return model;
        }

        protected virtual Dictionary<string, List<SemanticProperty>> FilterPropertySematicsByEntity(Dictionary<string, List<SemanticProperty>> semantics, MappingData mapData)
        {
            Dictionary<string, List<SemanticProperty>> filtered = new Dictionary<string, List<SemanticProperty>>();
            foreach (var item in semantics)
            {
                filtered.Add(item.Key, new List<SemanticProperty>());
                List<SemanticProperty> defaultProperties = new List<SemanticProperty>();
                foreach (var prop in item.Value)
                {
                    //Default prefix is always OK, but should be added last
                    if (String.IsNullOrEmpty(prop.Prefix))
                    {
                        defaultProperties.Add(prop);
                    }
                    else
                    {
                        //Filter out any properties belonging to other entities than the source entity
                        var entityData = GetEntityData(prop.Prefix, mapData.TargetEntitiesByPrefix, mapData.ParentDefaultPrefix);
                        if (entityData != null && mapData.EntityNames!=null)
                        {
                            if (mapData.EntityNames.Contains(entityData.Value.Key))
                            {
                                if (mapData.EntityNames[entityData.Value.Key].First() == entityData.Value.Value)
                                {
                                    filtered[item.Key].Add(prop);
                                }
                            }
                        }
                    }
                }
                filtered[item.Key].AddRange(defaultProperties);
            }
            return filtered;
        }

        private static IField GetFieldFromSemantics(MappingData mapData, SemanticProperty info)
        {
            var entityData = GetEntityData(info.Prefix, mapData.TargetEntitiesByPrefix, mapData.ParentDefaultPrefix);
            if (entityData != null)
            {
                // determine field semantics
                var vocab = entityData.Value.Key;
                string prefix = SemanticMapping.GetPrefix(vocab,WebRequestContext.Localization);
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

        private static KeyValuePair<string,string>? GetEntityData(string prefix, Dictionary<string,KeyValuePair<string,string>> entityData, string defaultPrefix)
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
            var bits = path.Split('/');
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

        private object GetFieldValues(IField field, Type propertyType, bool multival, MappingData mapData)
        {
            switch (field.FieldType)
            {
                case (FieldType.Date):
                    return GetDates(field, propertyType, multival);
                case (FieldType.Number):
                    return GetNumbers(field, propertyType, multival);
                case (FieldType.MultiMediaLink):
                    return GetMultiMediaLinks(field, propertyType, multival);
                case (FieldType.ComponentLink):
                    return GetMultiComponentLinks(field, propertyType, multival);
                case (FieldType.Embedded):
                    return GetMultiEmbedded(field, propertyType, multival, mapData);
                case (FieldType.Keyword):
                    return GetMultiKeywords(field, propertyType, multival);
                case (FieldType.Xhtml):
                    return GetMultiLineStrings(field, propertyType, multival);
                default:
                    return GetStrings(field, propertyType, multival);
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

        private static object GetDates(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(DateTime)))
            {
                if (multival)
                {
                    return field.DateTimeValues;
                }

                return field.DateTimeValues[0];
            }
            return null;
        }

        private static object GetNumbers(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(Double)))
            {
                if (multival)
                {
                    return field.NumericValues;
                }

                return field.NumericValues[0];
            }
            if (modelType.IsAssignableFrom(typeof(Int32)))
            {
                if (multival)
                {
                    return field.NumericValues.Select(d=>(int)Math.Round(d));
                }
                return (int)Math.Round(field.NumericValues[0]);
            }
            return null;
        }

        private static object GetMultiMediaLinks(IField field, Type modelType, bool multival)
        {
            return GetMultiMediaLinks(field.LinkedComponentValues, modelType, multival);
        }

        private static object GetMultiMediaLinks(IEnumerable<IComponent> items, Type modelType, bool multival)
        {
            var components = items as IList<IComponent> ?? items.ToList();
            if (components.Any())
            {
                // TODO TSI-247: find better way to determine image or video
                string schemaTitle = components.First().Schema.Title;
                if (modelType.IsAssignableFrom(typeof(YouTubeVideo)) && schemaTitle.ToLower().Contains("youtube"))
                {
                    if (multival)
                    {
                        return GetYouTubeVideos(components);
                    }

                    return GetYouTubeVideos(components)[0];
                }
                if (modelType.IsAssignableFrom(typeof(Download)) && schemaTitle.ToLower().Contains("download"))
                {
                    if (multival)
                    {
                        return GetDownloads(components);
                    }

                    return GetDownloads(components)[0];
                }
                if (modelType.IsAssignableFrom(typeof(Image)))
                {
                    if (multival)
                    {
                        return GetImages(components);
                    }

                    return GetImages(components)[0];
                }
                // TODO TSI-247: handle other types
            }
            return null;
        }

        private object GetMultiKeywords(IField field, Type linkedItemType, bool multival)
        {
            return GetMultiKeywords(field.Keywords, linkedItemType, multival);
        }

        private object GetMultiKeywords(IEnumerable<IKeyword> items, Type linkedItemType, bool multival)
        {
            //What to do depends on the target type
            if (linkedItemType == typeof(Tag))
            {
                List<Tag> res = items.Select(k => new Tag {DisplayText=GetKeywordDisplayText(k),Key=GetKeywordKey(k),TagCategory=k.TaxonomyId}).ToList();
                if (multival)
                {
                    return res;
                }

                return res[0];
            } 
            if (linkedItemType == typeof(bool))
            {
                //For booleans we assume the keyword key or value can be converted to bool
                List<bool> res = new List<bool>();
                foreach (var kw in items)
                {
                    bool val;
                    Boolean.TryParse(String.IsNullOrEmpty(kw.Key) ? kw.Title : kw.Key, out val);
                    res.Add(val);
                }
                if (multival)
                {
                    return res;
                }

                return res[0];
            }

            if (linkedItemType == typeof(String))
            {
                //List<String> res = items.Select(k=>GetKeywordDisplayText(k)).ToList();
                List<String> res = items.Select(GetKeywordDisplayText).ToList();
                if (multival)
                {
                    return res;
                }

                return res[0];
            }
            return null;
        }

        private static string GetKeywordKey(IKeyword k)
        {
            return !String.IsNullOrEmpty(k.Key) ? k.Key : k.Id;
        }

        private static string GetKeywordDisplayText(IKeyword k)
        {
            return !String.IsNullOrEmpty(k.Description) ? k.Description : k.Title;
        }
        
        private object GetMultiComponentLinks(IField field, Type linkedItemType, bool multival)
        {
            return GetMultiComponentLinks(field.LinkedComponentValues, linkedItemType, multival);
        }

        private object GetMultiComponentLinks(IEnumerable<IComponent> items, Type linkedItemType, bool multival)
        {
            //What to do depends on the target type
            if (linkedItemType == typeof(String) || linkedItemType == typeof(Link))
            {
                //For strings and Links, we simply resolve the link to a URL
                ILinkResolver linkResolver = SiteConfiguration.LinkResolver;
                List<String> urls = new List<String>();
                foreach (IComponent comp in items)
                {
                    var url = linkResolver.ResolveLink(comp.Id);
                    if (url != null)
                    {
                        urls.Add(url);
                    }
                }
                if (urls.Count == 0)
                {
                    return null;
                }
                if (linkedItemType == typeof(Link))
                {
                    if (multival)
                    {
                        return urls.Select(u => new Link { Url = u }).ToList();
                    }
                    return new Link { Url = urls[0] };
                }
                if (multival)
                {
                    return urls;
                }
                
                return urls[0];
            }

            //TODO is reflection the only way to do this?
            MethodInfo method = GetType().GetMethod("GetCompLink" + (multival ? "s" : String.Empty), BindingFlags.NonPublic | BindingFlags.Instance);
            method = method.MakeGenericMethod(new[] { linkedItemType });
            return method.Invoke(this, new object[] { items });
        }

        private object GetMultiEmbedded(IField field, Type propertyType, bool multival, MappingData mapData)
        {
            MappingData embedMapData = new MappingData
                {
                    TargetType = propertyType,
                    Meta = null,
                    EntityNames = mapData.EntityNames,
                    ParentDefaultPrefix = mapData.ParentDefaultPrefix,
                    TargetEntitiesByPrefix = mapData.TargetEntitiesByPrefix,
                    SemanticSchema = mapData.SemanticSchema,
                    EmbedLevel = mapData.EmbedLevel + 1
                };
            //This is a bit weird, but necessary as we cannot return List<object>, we need to get the right type
            var result = (IList)typeof(List<>).MakeGenericType(propertyType).GetConstructor(Type.EmptyTypes).Invoke(null);
            foreach (IFieldSet fields in field.EmbeddedValues)
            {
                embedMapData.Content = fields;
                var model = CreateModelFromMapData(embedMapData);
                if (model != null)
                {
                    result.Add(model);
                }
            }
            return result.Count == 0 ? null : (multival ? result : result[0]);

        }

        object GetMultiLineStrings(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(String)))
            {
                IRichTextProcessor richTextProcessor = SiteConfiguration.RichTextProcessor;
                if (multival)
                {
                    return field.Values.Select(v => richTextProcessor.ProcessRichText(v));
                }
                return richTextProcessor.ProcessRichText(field.Value);
            }
            return null;
        }

        static object GetStrings(IField field, Type modelType, bool multival)
        {
            if (modelType.IsAssignableFrom(typeof(String)))
            {
                if (multival)
                {
                    return field.Values;
                }
                return field.Value;
            }
            return null;
        }

        private static List<Image> GetImages(IEnumerable<IComponent> components)
        {
            return components.Select(c => new Image { Url = c.Multimedia.Url, FileName = c.Multimedia.FileName, FileSize = c.Multimedia.Size, MimeType = c.Multimedia.MimeType}).ToList();
        }

        private static List<YouTubeVideo> GetYouTubeVideos(IEnumerable<IComponent> components)
        {
            return components.Select(c => new YouTubeVideo { Url = c.Multimedia.Url, FileSize = c.Multimedia.Size, MimeType = c.Multimedia.MimeType, YouTubeId = c.MetadataFields["youTubeId"].Value }).ToList();
        }

        private static List<Download> GetDownloads(IEnumerable<IComponent> components)
        {
            //todo this contains hardcoded metadata while we would expect this to semantiaclly ma
            return components.Select(c => new Download { Url = c.Multimedia.Url, FileName = c.Multimedia.FileName, FileSize = c.Multimedia.Size, MimeType = c.Multimedia.MimeType, Description = (c.MetadataFields.ContainsKey("description") ? c.MetadataFields["description"].Value : null) }).ToList();
        }

        private List<T> GetCompLinks<T>(IEnumerable<IComponent> components) 
            where T: EntityModel
        {
            List<T> list = new List<T>();
            foreach (IComponent comp in components)
            {
                list.Add((T)CreateEntityModel(comp, typeof(T)));
            }
            return list;
        }

        /// <remarks>
        /// Called via reflection in <see cref="GetMultiComponentLinks(IEnumerable{IComponent}, Type, bool)"/>.
        /// </remarks>
        private T GetCompLink<T>(IEnumerable<IComponent> components)
            where T: EntityModel
        {
            return GetCompLinks<T>(components)[0];
        }

        protected Dictionary<string, string> GetAllFieldsAsDictionary(IComponent component)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            foreach (var fieldname in component.Fields.Keys)
            {
                if (!values.ContainsKey(fieldname))
                {
                    //special case for multival embedded name/value pair fields
                    if (fieldname == "settings" && component.Fields[fieldname].FieldType == FieldType.Embedded)
                    {
                        foreach (var embedFieldset in component.Fields[fieldname].EmbeddedValues)
                        {
                            var key = embedFieldset.ContainsKey("name") ? embedFieldset["name"].Value : null;
                            if (key != null)
                            {
                                var value = embedFieldset.ContainsKey("value") ? embedFieldset["value"].Value : null;
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
                        var val = component.Fields[fieldname].Value;
                        if (val != null)
                        {
                            values.Add(fieldname, val);
                        }
                    }
                }
            }
            foreach (var fieldname in component.MetadataFields.Keys)
            {
                if (!values.ContainsKey(fieldname))
                {
                    var val = component.MetadataFields[fieldname].Value;
                    if (val != null)
                    {
                        values.Add(fieldname, val);
                    }
                }
            }
            return values;
        }


        protected virtual string ProcessPageMetadata(IPage page, IDictionary<string,string> meta)
        {
            //First grab metadata from the page
            if (page.MetadataFields != null)
            {
                foreach (var field in page.MetadataFields.Values)
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
                foreach (var cp in page.ComponentPresentations)
                {
                    var region = GetRegionFromComponentPresentation(cp);
                    // determine title and description from first component in 'main' region
                    if (first && region.Name.Equals(TridionConfig.RegionForPageTitleComponent))
                    {
                        first = false;
                        IFieldSet metadata = cp.Component.MetadataFields;
                        IFieldSet fields = cp.Component.Fields;
                        if (metadata.ContainsKey(TridionConfig.StandardMetadataXmlFieldName) && metadata[TridionConfig.StandardMetadataXmlFieldName].EmbeddedValues.Count > 0)
                        {
                            IFieldSet standardMeta = metadata[TridionConfig.StandardMetadataXmlFieldName].EmbeddedValues[0];
                            if (title == null && standardMeta.ContainsKey(TridionConfig.StandardMetadataTitleXmlFieldName))
                            {
                                title = standardMeta[TridionConfig.StandardMetadataTitleXmlFieldName].Value;
                            }
                            if (description == null && standardMeta.ContainsKey(TridionConfig.StandardMetadataDescriptionXmlFieldName))
                            {
                                description = standardMeta[TridionConfig.StandardMetadataDescriptionXmlFieldName].Value;
                            }
                        }
                        if (title == null && fields.ContainsKey(TridionConfig.ComponentXmlFieldNameForPageTitle))
                        {
                            title = fields[TridionConfig.ComponentXmlFieldNameForPageTitle].Value;
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
            meta.Add("og:url", WebRequestContext.RequestUrl);
            //TODO is this always article?
            meta.Add("og:type", "article");
            meta.Add("og:locale", WebRequestContext.Localization.Culture);
            if (description != null)
            {
                meta.Add("og:description", description);
            }
            if (image != null)
            {
                image = WebRequestContext.Localization.GetBaseUrl() + image;
                meta.Add("og:image", image);
            }
            if (!meta.ContainsKey("description"))
            {
                meta.Add("description", description ?? title);
            }
            //TODO meta.Add("fb:admins", Configuration.GetConfig("core.fbadmins");
            return title + titlePostfix;
        }

        protected virtual void ProcessMetadataField(IField field, IDictionary<string, string> meta)
        {
            if (field.FieldType==FieldType.Embedded)
            {
                if (field.EmbeddedValues!=null && field.EmbeddedValues.Count>0)
                {
                    var subfields = field.EmbeddedValues[0];
                    foreach (var subfield in subfields.Values)
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

        private static RegionModel GetRegionFromComponentPresentation(IComponentPresentation cp)
        {
            string regionName = null;
            string module = SiteConfiguration.GetDefaultModuleName(); //Default module
            if (cp.ComponentTemplate.MetadataFields != null)
            {
                if (cp.ComponentTemplate.MetadataFields.ContainsKey("regionView"))
                {
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
            //Fallback if no meta - use the CT title
            if (regionName == null)
            {
                Match match = Regex.Match(cp.ComponentTemplate.Title, @".*?\[(.*?)\]");
                if (match.Success)
                {
                    regionName = match.Groups[1].Value;
                }
            }
            regionName = regionName ?? "Main";//default region name

            MvcData mvcData = new MvcData
            {
                AreaName = module, 
                ViewName = regionName, 
                ControllerName = SiteConfiguration.GetRegionController(), 
                ControllerAreaName = SiteConfiguration.GetDefaultModuleName(), 
                ActionName = SiteConfiguration.GetRegionAction()
            };
            return new RegionModel(regionName) { MvcData = mvcData };
        }

        private static RegionModel GetRegionFromIncludePage(IPage page)
        {
            string regionName = page.Title;

            return new RegionModel(regionName)
            {
                MvcData = new MvcData
                {
                    AreaName = SiteConfiguration.GetDefaultModuleName(),
                    ViewName = regionName,
                    ControllerName = SiteConfiguration.GetRegionController(),
                    ControllerAreaName = SiteConfiguration.GetDefaultModuleName(),
                    ActionName = SiteConfiguration.GetRegionAction()
                },
                XpmMetadata = new Dictionary<string, string>()
                {
                    {RegionModel.IncludedFromPageIdXpmMetadataKey, page.Id},
                    {RegionModel.IncludedFromPageTitleXpmMetadataKey, page.Title},
                    {RegionModel.IncludedFromPageFileNameXpmMetadataKey, page.Filename}
                }
            };
        }

        private static string GetDxaIdentifierFromTcmUri(string tcmUri)
        {
            // Return the Item (Reference) ID part of the TCM URI.
            return tcmUri.Split('-')[1];
        }

        /// <summary>
        /// Determine MVC data such as view, controller and area name from a Page
        /// </summary>
        /// <param name="page">The DD4T Page object</param>
        /// <returns>MVC data</returns>
        private static MvcData GetMvcData(IPage page)
        {
            string viewName = page.PageTemplate.Title.RemoveSpaces();
            if (page.PageTemplate.MetadataFields != null)
            {
                if (page.PageTemplate.MetadataFields.ContainsKey("view"))
                {
                    viewName = page.PageTemplate.MetadataFields["view"].Value;
                }
            }

            MvcData mvcData = CreateViewData(viewName);
            mvcData.ControllerName = SiteConfiguration.GetPageController();
            mvcData.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
            mvcData.ActionName = SiteConfiguration.GetPageAction();

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
            //Defaults
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
