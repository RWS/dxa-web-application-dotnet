using DD4T.ContentModel;
using HtmlAgilityPack;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.DD4T
{
    //TODO - abstract this away from DD4t
    public static class Markup
    {
        //TODO - this needs to be abstracted away...
        private static string PAGE_FORMAT = "<!-- Page Settings: {{\"PageID\":\"{0}\",\"PageModified\":\"{1}\",\"PageTemplateID\":\"{2}\",\"PageTemplateModified\":\"{3}\"}} -->";
        private static string PAGE_SCRIPT = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";
        private static string REGION_FORMAT = "<!-- Start Region: {{\"title\" : \"{0}\", \"allowedComponentTypes\" : [{{\"schema\" : \"{1}\", \"template\" : \"{2}\"}}], \"minOccurs\" : {3}, \"maxOccurs\" : {4}}} -->";//TODO improve according to https://code.google.com/p/tridion-practice/wiki/TridionUI2012FunctionsForUseInHtmlTemplates#Update
        private static string CP_FORMAT = "<!-- Start Component Presentation: {{\"ComponentID\" : \"{0}\", \"ComponentModified\" : \"{1}\", \"ComponentTemplateID\" : \"{2}\", \"ComponentTemplateModified\" : \"{3}\", \"IsRepositoryPublished\" : false}} -->";
        private static string FIELD_FORMAT = "<!-- Start Component Field: {{\"XPath\":\"{0}\"}} -->";
        private static string DATE_FORMAT = "yyyy-MM-ddTHH:mm:ss";

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
            return new MvcHtmlString(String.Format("typeof=\"{0}\" resource=\"{1}\"", "Region", region.Name));
        }

        public static MvcHtmlString GetInlineEditingBootstrap(IPage page)
        {
            if (Configuration.IsStaging)
            {
                var html = String.Format(PAGE_FORMAT, page.Id, page.RevisionDate.ToString(DATE_FORMAT), page.PageTemplate.Id, page.PageTemplate.RevisionDate.ToString(DATE_FORMAT)) + String.Format(PAGE_SCRIPT, Configuration.GetCmsUrl());
                return new MvcHtmlString(html);
            }
            return null;
        }

        public static MvcHtmlString Parse(MvcHtmlString result, IComponentPresentation cp)
        {
            if (Configuration.IsStaging)
            {

                //TODO abstract DD4T content model away, only process for preview requests
                //TODO extend for embedded fields/embedded components
                HtmlDocument html = new HtmlDocument();
                html.LoadHtml(String.Format("<html>{0}</html>", result.ToString()));
                var entity = html.DocumentNode.SelectSingleNode("//*[@data-componentid]");
                if (entity != null)
                {

                    //TODO remove attributes
                    string compId = ReadAndRemoveAttribute(entity, "data-componentid");
                    string compModified = ReadAndRemoveAttribute(entity, "data-componentmodified");
                    string templateId = ReadAndRemoveAttribute(entity, "data-componenttemplateid");
                    string templateModified = ReadAndRemoveAttribute(entity, "data-componenttemplatemodified");
                    HtmlCommentNode cpData = html.CreateComment(String.Format(CP_FORMAT, compId, compModified, templateId, templateModified));
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
                            HtmlCommentNode fieldData = html.CreateComment(String.Format(FIELD_FORMAT, xpath));
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
    }
}