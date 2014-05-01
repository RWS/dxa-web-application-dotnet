using DD4T.ContentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.DD4T.Extensions;
using System.Text.RegularExpressions;
using System.Collections;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using Sdl.Web.Mvc;

namespace Sdl.Web.DD4T
{
    /// <summary>
    /// Default ModelFactory, retrieves model type and sets properties
    /// according to some generic rules
    /// </summary>
    public class DD4TModelFactory : BaseModelFactory
    {
        public ExtensionlessLinkFactory LinkFactory { get; set; }
        public DD4TModelFactory()
        {
            this.LinkFactory = new ExtensionlessLinkFactory();
        }
        public override object CreateEntityModel(object data, string view = null)
        {
            IComponent component = data as IComponent;
            if (component==null && data is IComponentPresentation)
            {
                component = ((IComponentPresentation)data).Component;
            }
            if (component != null)
            {
                //TODO, handle more than just image MM components
                object model;
                if (component.Multimedia != null)
                {
                    model = GetImages(new List<IComponent> { component })[0];
                }
                else
                {
                    var entityType = component.Schema.RootElementName;
                    model = GetEntity(entityType);
                    var type = model.GetType();
                    foreach (var field in component.Fields)
                    {
                        SetProperty(model, field.Value);
                    }
                    foreach (var field in component.MetadataFields)
                    {
                        SetProperty(model, field.Value);
                    }
                }
                return model;
            }
            else
            {
                throw new Exception(String.Format("Cannot create model for class {0}. Expecting IComponentPresentation/IComponent.", data.GetType().FullName));
            }
        }

        public override object CreatePageModel(object data, string view = null, Dictionary<string,object> subPages = null)
        {
            IPage page = data as IPage;
            if (page != null)
            {
                WebPage model = new WebPage{Id=page.Id,Title=page.Title};
                foreach (var cp in page.ComponentPresentations)
                {
                    string regionName = GetRegionFromComponentPresentation(cp);
                    if (!model.Regions.ContainsKey(regionName))
                    {
                        model.Regions.Add(regionName, new Region { Name = regionName });
                    }
                    model.Regions[regionName].Items.Add(cp);
                }
                //Add header/footer
                if (subPages != null)
                {
                    if (subPages.ContainsKey("Header"))
                    {
                        WebPage headerPage = (WebPage)this.CreatePageModel(subPages["Header"]);
                        if (headerPage != null)
                        {
                            var header = new Header { Regions = new Dictionary<string, Region>() };
                            foreach (var region in headerPage.Regions)
                            {
                                //The main region should contain a Teaser containing the header logo etc.
                                if (region.Key == "Main")
                                {
                                    if (region.Value.Items.Count > 0)
                                    {
                                        Teaser headerTeaser = this.CreateEntityModel(region.Value.Items[0]) as Teaser;
                                        if (headerTeaser != null)
                                        {
                                            header.Logo = headerTeaser.Image;
                                            header.LogoLink = headerTeaser.Link;
                                            header.Heading = headerTeaser.Headline;
                                            header.Subheading = headerTeaser.Text;
                                        }
                                        else
                                        {
                                            Log.Warn("Header 'page' does not contain a Teaser in the Main region. Cannot set logo/heading/subheading");
                                        }
                                    }
                                }
                                //Other regions are simply added to the header regions container
                                else
                                {
                                    header.Regions.Add(region.Key, region.Value);
                                }
                            }
                            model.Header = header;
                        }
                    }
                    if (subPages.ContainsKey("Footer"))
                    {
                        WebPage footerPage = (WebPage)this.CreatePageModel(subPages["Footer"]);
                        if (footerPage != null)
                        {
                            var footer = new Footer { Regions = new Dictionary<string, Region>() };
                            foreach (var region in footerPage.Regions)
                            {
                                //The main region should contain a LinkList containing the footer copyright and links.
                                if (region.Key == "Main")
                                {
                                    if (region.Value.Items.Count > 0)
                                    {
                                        LinkList footerLinks = this.CreateEntityModel(region.Value.Items[0]) as LinkList;
                                        if (footerLinks != null)
                                        {
                                            footer.Copyright = footerLinks.Headline;
                                            footer.Links = footerLinks.Links;
                                        }
                                        else
                                        {
                                            Log.Warn("Footer 'page' does not contain a Teaser in the Main region. Cannot set logo/copyright");
                                        }
                                    }
                                }
                                //Other regions are simply added to the header regions container
                                else
                                {
                                    footer.Regions.Add(region.Key, region.Value);
                                }
                            }
                            model.Footer = footer;
                        }
                    }
                }


                return model;
            }
            throw new Exception(String.Format("Cannot create model for class {0}. Expecting IPage.", data.GetType().FullName));
        }

        private string GetRegionFromComponentPresentation(IComponentPresentation cp)
        {
            var match = Regex.Match(cp.ComponentTemplate.Title,@".*?\[(.*?)\]");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            //default region name
            return "Main";
        }

        public void SetProperty(object model, IField field)
        {
            if (field.Values.Count > 0 || (field.EmbeddedValues!=null && field.EmbeddedValues.Count > 0))
            {
                PropertyInfo pi = GetPropertyForField(model, field);
                if (pi != null)
                {
                    //TODO check/cast to the type we are mapping to 
                    bool multival = pi.PropertyType.IsGenericType && (pi.PropertyType.GetGenericTypeDefinition() == typeof(List<>));
                    Type propertyType = multival ? pi.PropertyType.GetGenericArguments()[0] : pi.PropertyType;
                    switch (field.FieldType)
                    {
                        case (FieldType.Date):
                            pi.SetValue(model, GetDates(field, propertyType, multival));
                            break;
                        case (FieldType.Number):
                            pi.SetValue(model, GetNumbers(field, propertyType, multival));
                            break;
                        case (FieldType.MultiMediaLink):
                            pi.SetValue(model, GetMultiMediaLinks(field, propertyType, multival));
                            break;
                        case (FieldType.ComponentLink):
                            pi.SetValue(model, GetMultiComponentLinks(field, propertyType, multival));
                            break;
                        case (FieldType.Embedded):
                            pi.SetValue(model, GetMultiEmbedded(field, propertyType, multival));
                            break;
                        default:
                            pi.SetValue(model, GetStrings(field, propertyType, multival));
                            break;
                    }
                }
            }
        }

        private PropertyInfo GetPropertyForField(object model, IField field)
        {
            //Default behaviour is to PascalCase the field xml name
            var propertyName = field.Name.Substring(0, 1).ToUpper() + field.Name.Substring(1);
            //Multivalue fields will typically have a non-pluralized field name (eg paragraph), but the property 
            //in the model is likely to be a List type property with a pluralized property name (eg Paragraphs)
            return model.GetType().GetProperty(propertyName) ??  model.GetType().GetProperty(propertyName + "s");
        }

        private object GetDates(IField field, Type modelType, bool multival)
        {
            if (typeof(DateTime).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.DateTimeValues;
                }
                else
                {
                    return field.DateTimeValues[0];
                }
            }
            return null;
        }

        private object GetNumbers(IField field, Type modelType, bool multival)
        {
            if (typeof(Double).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.NumericValues;
                }
                else
                {
                    return field.NumericValues[0];
                }
            }
            return null;
        }

        private object GetMultiMediaLinks(IField field, Type modelType, bool multival)
        {
            if (typeof(Image).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return GetImages(field.LinkedComponentValues);
                }
                else
                {
                    return GetImages(field.LinkedComponentValues)[0];
                }
            }
            return null;
        }


        private object GetMultiComponentLinks(IField field, Type modelType, bool multival)
        {
            if (multival)
            {
                return GetCompLinks(field.LinkedComponentValues);
            }
            else
            {
                return GetCompLinks(field.LinkedComponentValues)[0];
            }
        }

        private object GetMultiEmbedded(IField field, Type propertyType, bool multival)
        {
            if (propertyType == typeof(Link))
            {
                var links = GetLinks(field.EmbeddedValues);
                if (multival)
                {
                    return links;
                }
                else
                {
                    return links.Count > 0 ? links[0] : null;
                }
            }
            return null;
        }

        private object GetStrings(IField field, Type modelType, bool multival)
        {
            if (typeof(String).IsAssignableFrom(modelType))
            {
                if (multival)
                {
                    return field.Values;
                }
                else
                {
                    return field.Value;
                }
            }
            return null;
        }

        private List<Image> GetImages(IList<IComponent> components)
        {
            return components.Select(c => new Image { Url = c.Multimedia.Url, Id = c.Id, FileSize = c.Multimedia.Size }).ToList();
        }

        private List<object> GetCompLinks(IList<IComponent> components)
        {
            return components.Select(c => this.CreateEntityModel(c)).ToList();
        }

        private List<Link> GetLinks(IList<IFieldSet> list)
        {
            var result = new List<Link>();
            foreach (IFieldSet fs in list)
            {
                var link = new Link();
                link.AlternateText = fs.ContainsKey("alternateText") ? fs["alternateText"].Value : null;
                link.LinkText = fs.ContainsKey("linkText") ? fs["linkText"].Value : null;
                link.Url = fs.ContainsKey("externalLink") ? fs["externalLink"].Value : (fs.ContainsKey("internalLink") ? LinkFactory.ResolveExtensionlessLink(fs["internalLink"].LinkedComponentValues[0].Id) : null);
                if (!String.IsNullOrEmpty(link.Url))
                {
                    result.Add(link);
                }
            }
            return result;
        }


     }
}
