using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.Core.Contracts.ViewModels;
using Dynamic = DD4T.ContentModel;
using DD4T.Mvc.SiteEdit;
using DD4T.ContentModel;
using DD4T.Mvc.ViewModels.XPM;
using DD4T.ContentModel.Contracts.Configuration;

namespace DD4T.Mvc.ViewModels.XPM
{
    /// <summary>
    /// XPM Markup Service for the DD4T implementation
    /// </summary>
    public class XpmMarkupService : IXpmMarkupService
    {
        private readonly IDD4TConfiguration _configuration;
        public XpmMarkupService(IDD4TConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            _configuration = configuration;
        }
        public string RenderXpmMarkupForField(IField field, int index = -1)
        {     
            var result = index >= 0 ? XPMTags.GenerateSiteEditFieldTag(field, index)
                            : XPMTags.GenerateSiteEditFieldTag(field);
            return result ?? string.Empty;
        }

        public string RenderXpmMarkupForComponent(IComponentPresentation cp)
        {
            return XPMTags.GenerateSiteEditComponentTag(cp);
        }
        public string RenderXpmMarkupForPage(IPage page, string url)
        {
            return XPMTags.GenerateSiteEditPageTag(page, url);
        }

        public bool IsSiteEditEnabled()
        {
            return _configuration.IsPreview;
        }

       

        internal class XPMTags
        {
            /// <summary>
            /// string format used to create UI 2012 page level tag.
            /// </summary>
            public static string PageSeFormat = "<!-- Page Settings: {{" +
                    "\"PageID\":\"{0}\"," +              // page tcm uri
                    "\"PageModified\":\"{1}\"," +        // page modified date (2012-04-05T17:33:02)
                    "\"PageTemplateID\":\"{2}\"," +      // page template tcm uri
                    "\"PageTemplateModified\":\"{3}\"" + // page template modified date (2012-04-05T17:33:02)
                "}} -->";

            /// <summary>
            /// string format representing UI 2012 component level tag.
            /// </summary>
            public static string ComponentSeFormat = "<!-- Start Component Presentation: {{" +
                    "\"ComponentID\" : \"{0}\", " +               // component tcm uri
                    "\"ComponentModified\" : \"{1}\", " +         // component modified date (2012-04-05T17:33:02)
                    "\"ComponentTemplateID\" : \"{2}\", " +       // component template id
                    "\"ComponentTemplateModified\" : \"{3}\", " + // component template modified date (2012-04-05T17:33:02)
                    "\"IsRepositoryPublished\" : {4}" +           // is repository published (true if dynamic component template, false otherwise)
                    "{5}" +                                       // is query based (true for a broker queried dcp, omit if component presentation is embedded on a page)
                "}} -->";


            /// <summary>
            /// string format representing UI 2012 field marking.
            /// </summary>
            public static string FieldSeFormat = "<!-- Start Component Field: {{\"XPath\":\"{0}\"}} -->"; // xpath of the field

            /// <summary>
            /// string format representing UI 2012 region marking.
            /// </summary>
            public static string RegionSeFormat = "<!-- Start Region: {{ \"title\": \"{0}\", " +
                    "\"allowedComponentTypes\": [{{ " +
                        "\"schema\": \"{1}\", " + // schema tcm uri
                        "\"template\": \"{2}\"" + // component template tcm uri
                    " }}], " +
                    "\"minOccurs\": {3}, " +      // minimum amount of components in this region
                    "\"maxOccurs\": {4} " +       // maximum amount of components in this region
                "}} -->";

            /// <summary>
            /// string format representing UI 2012 bootstrap script.
            /// </summary>
            public static string BootStrap = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";


           
            /// <summary>
            /// Generates SiteEdit tag for given page.
            /// </summary>
            /// <param name="page">Page the tag belongs to.</param>
            /// <returns>string representing the JSON SiteEdit tag.</returns>
            public static string GenerateSiteEditPageTag(IPage page, string tridionHostUrl)
            {
                string result = string.Format(PageSeFormat, page.Id, string.Format("{0:s}", page.RevisionDate), page.PageTemplate.Id, string.Format("{0:s}", page.PageTemplate.RevisionDate));
                result += string.Format(BootStrap, tridionHostUrl);
                return result;

            }

            /// <summary>
            /// It is possible to mark regions in your Page, so that only Components of a certain Schema can be dropped in there and the correct Component Template will automatically be applied to them.
            /// </summary>
            /// <param name="title">ContentType name or region title</param>
            /// <param name="minOccurs">minimum amount of components in this region</param>
            /// <param name="maxOccurs">maximum amount of components in this region</param>
            /// <param name="schemaUri">allowed schema tcm uri</param>
            /// <param name="templateUri">component template uri</param>
            /// <returns>string representing the JSON SiteEdit tag.</returns>
            public static string GenerateSiteEditRegionTag(string title, int minOccurs, int maxOccurs, string schemaUri, string templateUri)
            {
                return string.Format(RegionSeFormat, title, schemaUri, templateUri, minOccurs, maxOccurs);
            }

            /// <summary>
            /// Generates a SiteEdit tag for a componentpresentation. It also needs to know which region it's in (for component
            /// swapping) and the order of the page (for a true unique ID).
            /// </summary>
            /// <param name="cp">The componentpresentation to mark.</param>
            /// <returns>string representing the JSON SiteEdit tag.</returns>
            public static string GenerateSiteEditComponentTag(IComponentPresentation cp)
            {
                // is query based tells us if the dcp was the result of a broker query and the component presentation is not embedded on the page
                string isQueryBased = cp.IsDynamic ? ", \"IsQueryBased\" : true" : string.Empty;
                return string.Format(ComponentSeFormat, cp.Component.Id, string.Format("{0:s}", cp.Component.RevisionDate), cp.ComponentTemplate.Id, string.Format("{0:s}", cp.ComponentTemplate.RevisionDate), cp.IsDynamic.ToString().ToLower(), isQueryBased);

            }

            /// <summary>
            /// Function generates a fieldmarking for a single-value SiteEditable field based on field name and xpath. 
            /// For multi-value fields, please code the JSON yourself.
            /// </summary>
            /// <param name="fieldname">the name of the field</param>
            /// <param name="xpath">xpath</param>
            /// <returns>string representing the JSON SiteEdit tag.</returns>
            public static string GenerateSiteEditFieldMarkingWithXpath(string xpath)
            {

                return string.Format(FieldSeFormat, xpath);
            }

            /// <summary>
            /// generates siteedit field tag
            /// </summary>
            /// <param name="field">the field to mark</param>
            /// <returns>siteedit field tag</returns>
            public static string GenerateSiteEditFieldTag(IField field)
            {
                if (string.IsNullOrEmpty(field.XPath))
                {
                    return string.Empty;
                }
                return string.Format(FieldSeFormat, field.XPath);


            }

            /// <summary>
            /// generates siteedit field tag for a specific instance of multivalue field.
            /// </summary>
            /// <param name="field">the field to mark</param>
            /// <param name="mvOrder">order of the multivalue instance (zero based)</param>
            /// <returns>siteedit field tag</returns>
            public static string GenerateSiteEditFieldTag(IField field, int mvOrder)
            {
                if (string.IsNullOrEmpty(field.XPath))
                {
                    return string.Empty;
                }
                string xpath = string.Format("{0}[{1}]", field.XPath, mvOrder + 1);

                return string.Format(FieldSeFormat, xpath);

            }

        }
    }
}
