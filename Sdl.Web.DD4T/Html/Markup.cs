using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using DD4T.ContentModel;
using HtmlAgilityPack;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;

namespace Sdl.Web.DD4T
{
    //TODO - abstract this away from DD4t
    public static class Markup
    {
        //TODO - this needs to be abstracted away...
        private const string PageFormat = "<!-- Page Settings: {{\"PageID\":\"{0}\",\"PageModified\":\"{1}\",\"PageTemplateID\":\"{2}\",\"PageTemplateModified\":\"{3}\"}} -->";
        private const string PageScript = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";
        private const string RegionFormat = "<!-- Start Region: {{title: \"{0}\", allowedComponentTypes: [{1}], minOccurs: {2}{3}}} -->";
        private const string ComponentTypeFormat = "{2}{{schema: \"{0}\", template: \"{1}\"}}";
        private const string MaxOccursFormat = ", maxOccurs: {0}";
        private const string CpFormat = "<!-- Start Component Presentation: {{\"ComponentID\" : \"{0}\", \"ComponentModified\" : \"{1}\", \"ComponentTemplateID\" : \"{2}\", \"ComponentTemplateModified\" : \"{3}\", \"IsRepositoryPublished\" : false}} -->";
        private const string FieldFormat = "<!-- Start Component Field: {{\"XPath\":\"{0}\"}} -->";
        private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";

        public static MvcHtmlString Entity(Entity entity)
        {
            StringBuilder data = new StringBuilder();
            var prefixes = new Dictionary<string,string>();
            var entityTypes = new List<string>();
            foreach(SemanticEntityAttribute attribute in entity.GetType().GetCustomAttributes(true).Where(a=>a is SemanticEntityAttribute).ToList())
            {
                var prefix = attribute.Prefix;
                if (!String.IsNullOrEmpty(prefix))
                {
                    if (!prefixes.ContainsKey(prefix))
                    {
                        prefixes.Add(prefix,attribute.Vocab);
                    }
                    entityTypes.Add(String.Format("{0}:{1}", prefix, attribute.EntityName));
                }
            }
            if (prefixes != null && prefixes.Count > 0)
            {
                data.AppendFormat("prefix=\"{0}\" typeof=\"{1}\"", String.Join(" ", prefixes.Select(p=>String.Format("{0}: {1}",p.Key,p.Value))), String.Join(" ", entityTypes)) ;
            }
            if (Configuration.IsStaging)
            {
                foreach (var item in entity.EntityData)
                {
                    data.AppendFormat("data-{0}=\"{1}\"", item.Key, HttpUtility.HtmlAttributeEncode(item.Value));
                }
            }
            return new MvcHtmlString(data.ToString());
        }
        
        public static MvcHtmlString Property(Entity entity, string property, int index = 0)
        {
            StringBuilder data = new StringBuilder();
            var pi = entity.GetType().GetProperty(property);
            if (pi != null)
            {
                var propertyTypes = pi.GetCustomAttributes(true).Where(a => a is SemanticPropertyAttribute).Select(s => ((SemanticPropertyAttribute)s).PropertyName).ToArray();
                if (propertyTypes != null && propertyTypes.Length > 0)
                {
                    data.AppendFormat("typeof=\"{0}\"", String.Join(" ", propertyTypes));
                }
                if (Configuration.IsStaging)
                {
                    if (entity.PropertyData.ContainsKey(property))
                    {
                        var xpath = entity.PropertyData[property];
                        var suffix = xpath.EndsWith("]") ? "" : String.Format("[{0}]", index+1);
                        data.AppendFormat("data-xpath=\"{0}{1}\"", HttpUtility.HtmlAttributeEncode(xpath), suffix);
                    }
                }
            }
            return new MvcHtmlString(data.ToString());        
        }

        public static MvcHtmlString Region(Region region)
        {
            var data = String.Empty;
            if (Configuration.IsStaging)
            {
                data = String.Format(" data-region=\"{0}\"", region.Name);
            }

            return new MvcHtmlString(String.Format("typeof=\"{0}\" resource=\"{1}\"{2}", "Region", region.Name, data));
        }

        public static MvcHtmlString GetInlineEditingBootstrap(IPage page)
        {
            if (Configuration.IsStaging)
            {
                var html = String.Format(PageFormat, page.Id, page.RevisionDate.ToString(DateFormat), page.PageTemplate.Id, page.PageTemplate.RevisionDate.ToString(DateFormat)) + String.Format(PageScript, Configuration.GetCmsUrl());
                return new MvcHtmlString(html);
            }
            return null;
        }

        public static MvcHtmlString Parse(MvcHtmlString result, Region region)
        {
            if (Configuration.IsStaging)
            {
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(String.Format("<html>{0}</html>", result));
                var entity = html.DocumentNode.SelectSingleNode("//*[@data-region]");
                if (entity != null)
                {
                    string name = ReadAndRemoveAttribute(entity, "data-region");

                    // TODO determine min occurs and max occurs for the region
                    HtmlCommentNode regionData = html.CreateComment(MarkRegion(name));
                    entity.ChildNodes.Insert(0, regionData);
                }

                return new MvcHtmlString(html.DocumentNode.SelectSingleNode("/html").InnerHtml);
            }
            return result;
        }

        public static MvcHtmlString Parse(MvcHtmlString result, IComponentPresentation cp)
        {
            if (Configuration.IsStaging)
            {
                //TODO abstract DD4T content model away, only process for preview requests
                //TODO extend for embedded fields/embedded components
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(String.Format("<html>{0}</html>", result));
                var entity = html.DocumentNode.SelectSingleNode("//*[@data-componentid]");
                if (entity != null)
                {

                    //TODO remove attributes
                    string compId = ReadAndRemoveAttribute(entity, "data-componentid");
                    string compModified = ReadAndRemoveAttribute(entity, "data-componentmodified");
                    string templateId = ReadAndRemoveAttribute(entity, "data-componenttemplateid");
                    string templateModified = ReadAndRemoveAttribute(entity, "data-componenttemplatemodified");
                    HtmlCommentNode cpData = html.CreateComment(String.Format(CpFormat, compId, compModified, templateId, templateModified));
                    entity.ChildNodes.Insert(0, cpData);
                    //string lastProperty = "";
                    //int index = 1;
                    var properties = entity.SelectNodes("//*[@data-xpath]");
                    if (properties != null && properties.Count > 0)
                    {
                        foreach (var property in properties)
                        {
                            var xpath = ReadAndRemoveAttribute(property, "data-xpath");
                            //TODO index of mv fields
                            //index = propName == lastProperty ? index+1 : 1;
                            //lastProperty = propName;
                            HtmlCommentNode fieldData = html.CreateComment(String.Format(FieldFormat, xpath));
                            if (property.HasChildNodes)
                            {
                                property.ChildNodes.Insert(0, fieldData);
                            }
                            else
                            {
                                property.ParentNode.InsertBefore(fieldData, property);
                            }
                        }
                    }
                }

                return new MvcHtmlString(html.DocumentNode.SelectSingleNode("/html").InnerHtml);
            }
            return result;
        }

        private static string ReadAndRemoveAttribute(HtmlNode entity, string name)
        {
            if (entity.Attributes.Contains(name))
            {
                var attr = entity.Attributes[name];
                entity.Attributes.Remove(attr);
                return attr.Value;
            }
            return null;
        }

        private static string MarkRegion(string name, int minOccurs = 0, int maxOccurs = 0)
        {
            XpmRegion xpmRegion = SemanticMapping.GetXpmRegion(name);
            StringBuilder allowedComponentTypes = new StringBuilder();
            string separator = String.Empty;
            bool first = true;
            foreach (var componentTypes in xpmRegion.ComponentTypes)
            {
                allowedComponentTypes.AppendFormat(ComponentTypeFormat, componentTypes.Schema, componentTypes.Template, separator);
                if (first)
                {
                    first = false;
                    separator = ", ";
                }
            }

            string maxOccursElement = String.Empty;
            if (maxOccurs > 0)
            {
                maxOccursElement = String.Format(MaxOccursFormat, maxOccurs);
            }

            return String.Format(RegionFormat, name, allowedComponentTypes, minOccurs, maxOccursElement);
        }
    }
}