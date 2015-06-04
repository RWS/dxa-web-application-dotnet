using HtmlAgilityPack;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Tridion.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sdl.Web.Tridion.Markup
{
    public static class TridionMarkup
    {
        private const string PageFormat = "<!-- Page Settings: {{\"PageID\":\"{0}\",\"PageModified\":\"{1}\",\"PageTemplateID\":\"{2}\",\"PageTemplateModified\":\"{3}\"}} -->";
        private const string PageScript = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";
        private const string RegionFormat = "<!-- Start Region: {{title: \"{0}\", allowedComponentTypes: [{1}], minOccurs: {2}{3}}} -->";
        private const string ComponentTypeFormat = "{2}{{schema: \"{0}\", template: \"{1}\"}}";
        private const string MaxOccursFormat = ", maxOccurs: {0}";
        private const string ComponentPresentationFormat = "<!-- Start Component Presentation: {{\"ComponentID\" : \"{0}\", \"ComponentModified\" : \"{1}\", \"ComponentTemplateID\" : \"{2}\", \"ComponentTemplateModified\" : \"{3}\", \"IsRepositoryPublished\" : {4}}} -->";
        private const string IsQueryBased = "true, \"IsQueryBased\" : true";
        private const string FieldFormat = "<!-- Start Component Field: {{\"XPath\":\"{0}\"}} -->";
        //private const string DateFormat = "yyyy-MM-ddTHH:mm:ss";
        private const string NullUri = "tcm:0-0-0";
        private const string Epoch = "1970-01-01T00:00:00";

        public static string ParseRegion(string regionHtml, Localization loc)
        {
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(String.Format("<html>{0}</html>", regionHtml));
            HtmlNode entity = html.DocumentNode.SelectSingleNode("//*[@data-region]");
            if (entity != null)
            {
                string name = ReadAndRemoveAttribute(entity, "data-region");

                // TODO determine min occurs and max occurs for the region
                HtmlCommentNode regionData = html.CreateComment(MarkRegion(name,loc));
                entity.ChildNodes.Insert(0, regionData);
            }
            return html.DocumentNode.SelectSingleNode("/html").InnerHtml;
        }

        public static string ParseEntity(string entityHtml)
        {
            //HTML Agility pack drops closing option tags for some reason (bug?)
            HtmlNode.ElementsFlags.Remove("option");
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(String.Format("<html>{0}</html>", entityHtml));
            HtmlNodeCollection entities = html.DocumentNode.SelectNodes("//*[@data-componentid]");
            string dummyTemplateId = NullUri;
            string dummyTemplateModified = Epoch;
            string isRepositoryPublished = "false";
            if (entities != null)
            {
                foreach (HtmlNode entity in entities)
                {
                    string compId = ReadAndRemoveAttribute(entity, "data-componentid");
                    string compModified = ReadAndRemoveAttribute(entity, "data-componentmodified", Epoch);
                    string templateId = ReadAndRemoveAttribute(entity, "data-componenttemplateid", NullUri);
                    string templateModified = ReadAndRemoveAttribute(entity, "data-componenttemplatemodified", Epoch);
                    // store template id as dummy default for next round (all our component templates generate the same output anyways)
                    if (!templateId.Equals(NullUri))
                    {
                        dummyTemplateId = templateId;
                        dummyTemplateModified = templateModified;
                    }
                    else
                    {
                        // XPM does not like null uris for templates, so use defaults set before
                        templateId = dummyTemplateId;
                        templateModified = dummyTemplateModified;
                        // using a dummy template, so this should be considered a dynamic cp
                        isRepositoryPublished = IsQueryBased;
                    }
                    if (!String.IsNullOrEmpty(compId))
                    {
                        HtmlCommentNode cpData = html.CreateComment(String.Format(ComponentPresentationFormat, compId, compModified, templateId, templateModified, isRepositoryPublished));
                        entity.ChildNodes.Insert(0, cpData);
                    }
                    //string lastProperty = "";
                    //int index = 1;
                    HtmlNodeCollection properties = entity.SelectNodes("//*[@data-xpath]");
                    if (properties != null && properties.Count > 0)
                    {
                        foreach (HtmlNode property in properties)
                        {
                            string xpath = ReadAndRemoveAttribute(property, "data-xpath");
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
            }
            return  html.DocumentNode.SelectSingleNode("/html").InnerHtml;
        }

        private static string ReadAndRemoveAttribute(HtmlNode entity, string name, string defaultValue = null)
        {
            if (entity.Attributes.Contains(name))
            {
                HtmlAttribute attr = entity.Attributes[name];
                entity.Attributes.Remove(attr);
                return attr.Value;
            }
            return defaultValue;
        }

        private static string MarkRegion(string name, Localization loc, int minOccurs = 0, int maxOccurs = 0)
        {
            XpmRegion xpmRegion = TridionConfig.GetXpmRegion(name, loc);
            if (xpmRegion == null)
            {
                return String.Empty;
            }

            StringBuilder allowedComponentTypes = new StringBuilder(); 
            string separator = String.Empty;
            bool first = true;
            foreach (ComponentType componentTypes in xpmRegion.ComponentTypes)
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

        public static string PageMarkup(IDictionary<string,string> pageData)
        {
            string pageId = pageData.ContainsKey("PageID") ? pageData["PageID"] : null;
            string pageTemplateId = pageData.ContainsKey("PageTemplateID") ? pageData["PageTemplateID"] : null;
            string pageDate = pageData.ContainsKey("PageModified") ? pageData["PageModified"] : null;
            string pageTemplateDate = pageData.ContainsKey("PageTemplateModified") ? pageData["PageTemplateModified"] : null;
            string cmsUrl = pageData.ContainsKey("CmsUrl") ? pageData["CmsUrl"] : null;
            // remove trailing slash from cmsUrl if available
            if (!String.IsNullOrEmpty(cmsUrl) && cmsUrl.EndsWith("/"))
            {
                cmsUrl = cmsUrl.Remove(cmsUrl.Length - 1);
            }
            return String.Format(PageFormat, pageId, pageDate, pageTemplateId, pageTemplateDate) + String.Format(PageScript, cmsUrl);
        }
    }
}
