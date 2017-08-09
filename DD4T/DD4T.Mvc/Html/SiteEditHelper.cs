using System.Web.Mvc;
using DD4T.ContentModel;
using DD4T.Mvc.SiteEdit;

namespace DD4T.Mvc.Html
{
    public static class SiteEditHelper
    {
        public static MvcHtmlString SiteEditPage(this HtmlHelper helper, IPage page)
        {
            return new MvcHtmlString(SiteEdit.SiteEditService.GenerateSiteEditPageTag(page));
        }
        public static MvcHtmlString SiteEditComponentPresentation(this HtmlHelper helper, IComponentPresentation componentPresentation)
        {
            return SiteEditComponentPresentation(helper, componentPresentation, "default");
        }
        public static MvcHtmlString SiteEditComponentPresentation(this HtmlHelper helper, IComponent component, string componentTemplateId, string region)
        {
            return SiteEditComponentPresentation(helper, component, componentTemplateId, false, region);
        }
        public static MvcHtmlString SiteEditComponentPresentation(this HtmlHelper helper, IComponent component, string componentTemplateId, bool queryBased, string region)
        {
            ComponentTemplate ct = new ComponentTemplate();
            ct.Id = componentTemplateId;
            ComponentPresentation cp = new ComponentPresentation();
            cp.Component = component as Component;
            cp.ComponentTemplate = ct;
            cp.OrderOnPage = -1;
            return SiteEditComponentPresentation(helper, cp, queryBased, region);
        }
        public static MvcHtmlString SiteEditComponentPresentation(this HtmlHelper helper, IComponentPresentation componentPresentation, string region)
        {
            return SiteEditComponentPresentation(helper, componentPresentation, false, region);
        }
        public static MvcHtmlString SiteEditComponentPresentation(this HtmlHelper helper, IComponentPresentation componentPresentation, bool queryBased, string region)
        {
            return new MvcHtmlString(SiteEdit.SiteEditService.GenerateSiteEditComponentTag(componentPresentation, queryBased, region));
        }
        public static MvcHtmlString SiteEditField(this HtmlHelper helper, IComponent component, IField field)
        {
            return SiteEditField(helper, component, field, -1);
        }

        public static MvcHtmlString SiteEditField(this HtmlHelper helper, IComponent component, IField field, int index)
        {
            if (SiteEditService.IsSiteEditEnabled(component))
            {
                if (index == -1)
                    return new MvcHtmlString(SiteEditService.GenerateSiteEditFieldTag(field, 0));
                else
                    return new MvcHtmlString(SiteEditService.GenerateSiteEditFieldTag(field, index));
            }
            return new MvcHtmlString(string.Empty);
        }
    }
}
