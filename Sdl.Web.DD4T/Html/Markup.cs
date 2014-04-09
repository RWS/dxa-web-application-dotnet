using DD4T.ContentModel;
using HtmlAgilityPack;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private static string REGION_FORMAT = "";//TODO
        private static string CP_FORMAT = "<!-- Start Component Presentation: {{\"ComponentID\" : \"{0}\", \"ComponentModified\" : \"{1}\", \"ComponentTemplateID\" : \"{2}\", \"ComponentTemplateModified\" : \"{2}\", \"IsRepositoryPublished\" : false}} -->";
        private static string FIELD_FORMAT = "<!-- Start Component Field: {{\"XPath\":\"tcm:Content/custom:{0}/custom:{1}\"}} -->";
        private static string DATE_FORMAT = "s";

        public static MvcHtmlString Entity(Entity entity)
        {
            return new MvcHtmlString(String.Format("vocab=\"{0}\" typeof=\"{1}\"", entity.Semantics.Vocabulary, entity.Semantics.Type));
        }
        public static MvcHtmlString Property(Entity entity, string property)
        {
            return new MvcHtmlString(String.Format("property=\"{0}{1}\"", property.Substring(0, 1).ToLower(), property.Substring(1)));
        }
        public static MvcHtmlString Region(Region region)
        {
            return new MvcHtmlString(String.Format("typeof=\"{0}\" resource=\"{1}\"", "Region", region.Name));
        }

        public static MvcHtmlString GetInlineEditingBootstrap(IPage page)
        {
            var html = String.Format(PAGE_FORMAT, page.Id, page.RevisionDate.ToString("s"), page.PageTemplate.Id, page.PageTemplate.RevisionDate) + String.Format(PAGE_SCRIPT, Configuration.GetCmsUrl());
            return new MvcHtmlString(html);
        }

        public static MvcHtmlString Parse(MvcHtmlString result, IComponentPresentation cp)
        {
            //TODO abstract DD4T content model away, only process for staging
            HtmlDocument html = new HtmlDocument();
            html.LoadHtml(String.Format("<html>{0}</html>", result.ToString()));
            var entity = html.DocumentNode.SelectSingleNode("//*[@typeof='Article']");
            if (entity != null)
            {
                HtmlCommentNode cpData = html.CreateComment(String.Format(CP_FORMAT, cp.Component.Id, cp.Component.RevisionDate.ToString(DATE_FORMAT), cp.ComponentTemplate.Id, cp.ComponentTemplate.RevisionDate.ToString(DATE_FORMAT)));
                entity.ChildNodes.Insert(0, cpData);
                foreach (var property in entity.SelectNodes("//*[@property]"))
                {
                    var propName = property.Attributes["property"].Value;
                    HtmlCommentNode fieldData = html.CreateComment(String.Format(FIELD_FORMAT, "Article", propName));
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
            return new MvcHtmlString(html.DocumentNode.SelectSingleNode("/html").InnerHtml);
        }
    }
}