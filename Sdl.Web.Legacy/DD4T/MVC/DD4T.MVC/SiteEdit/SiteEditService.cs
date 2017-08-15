using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel;

namespace DD4T.Mvc.SiteEdit
{
    /// <summary>
    /// The class, with static methods only, serves views and controllers with SiteEdit tags. 
    /// The point of bundling them is to keep all SiteEdit behaviour in a single point, 
    /// and hence its strange JSON codes accessible to non-Tridion specialized developers.
    /// 
    /// author Rogier Oudshoorn, .Net port by Quirijn Slings, UI 2012 port by Bart Koopman
    /// </summary>
    [Obsolete("use DD4T.Mvc.ViewModels.XPM.XpmMarkupService")]
    public class SiteEditService
    {
        /// <summary>
        /// SiteEdit settings object
        /// </summary>
        public static SiteEditSettings SiteEditSettings = new SiteEditSettings();

        /// <summary>
        /// string Format used to create the Page-level SiteEdit tags.
        /// </summary>
        public static string PageSeFormat = "<!-- SiteEdit Settings: {{" +
                "\"PageID\":\"{0}\", " +                         // page id
                "\"PageVersion\":{1}, " +                        // page version
                "\"ComponentPresentationLocation\":1, " +        // add components to bottom of list, rather then front of list
                "\"BluePrinting\" : {{" +
                    "\"PageContext\" : \"tcm:0-{2}-1\", " +      // point to publication where pages must be created
                    "\"ComponentContext\" : \"tcm:0-{3}-1\", " + // point to publication where components must be created                
                    "\"PublishContext\" : \"tcm:0-{4}-1\" " +    // point to publication where page is published                
                "}}" +
            "}} -->";

        /// <summary>
        /// string format used to create UI 2012 page level tag.
        /// </summary>
        public static string Ui2012PageSeFormat = "<!-- Page Settings: {{" +
                "\"PageID\":\"{0}\"," +              // page tcm uri
                "\"PageModified\":\"{1}\"," +        // page modified date (2012-04-05T17:33:02)
                "\"PageTemplateID\":\"{2}\"," +      // page template tcm uri
                "\"PageTemplateModified\":\"{3}\"" + // page template modified date (2012-04-05T17:33:02)
            "}} -->";

        /// <summary>
        /// string Format representing a Component-level SiteEdit tag.
        /// </summary>
        public static string ComponentSeFormat = "<!-- Start SiteEdit Component Presentation: {{" +
                "\"ID\" : \"CP{0}\", " +                // unique id
                "\"ComponentID\" : \"{1}\", " +         // comp id
                "\"ComponentTemplateID\" : \"{2}\", " + // ct id
                "\"ComponentVersion\" : {3}, " +        // comp version
                "\"IsQueryBased\" : {4}, " +            // query will be true for lists; out of scope for now
                "\"SwapLabel\" : \"{5}\" " +            // label with which components can be swapped; so region
            "}} -->";

        /// <summary>
        /// string format representing UI 2012 component level tag.
        /// </summary>
        public static string Ui2012ComponentSeFormat = "<!-- Start Component Presentation: {{" +
                "\"ComponentID\" : \"{0}\", " +               // component tcm uri
                "\"ComponentModified\" : \"{1}\", " +         // component modified date (2012-04-05T17:33:02)
                "\"ComponentTemplateID\" : \"{2}\", " +       // component template id
                "\"ComponentTemplateModified\" : \"{3}\", " + // component template modified date (2012-04-05T17:33:02)
                "\"IsRepositoryPublished\" : {4}" +           // is repository published (true if dynamic component template, false otherwise)
                "{5}" +                                       // is query based (true for a broker queried dcp, omit if component presentation is embedded on a page)
            "}} -->";
        

        /// <summary>
        /// string Format representing a simple, non-multivalue SiteEdit field marking.
        /// </summary>
        public static string FieldSeFormat =  "<!-- Start SiteEdit Component Field: {{" +
                "\"ID\" : \"{0}\", " +        // id (name) of the field
                "\"IsMultiValued\" : {1}, " + // multivalue?
                "\"XPath\" : \"{2}\" " +      // xpath of the field
            "}} -->";

        /// <summary>
        /// string format representing UI 2012 field marking.
        /// </summary>
        public static string Ui2012FieldSeFormat = "<!-- Start Component Field: {{\"XPath\":\"{0}\"}} -->"; // xpath of the field

        /// <summary>
        /// string format representing UI 2012 region marking.
        /// </summary>
        public static string Ui2012RegionSeFormat = "<!-- Start Region: {{ \"title\": \"{0}\", " + 
                "\"allowedComponentTypes\": [{{ "+
                    "\"schema\": \"{1}\", " + // schema tcm uri
                    "\"template\": \"{2}\"" + // component template tcm uri
                " }}], " +
                "\"minOccurs\": {3}, " +      // minimum amount of components in this region
                "\"maxOccurs\": {4} " +       // maximum amount of components in this region
            "}} -->";

        /// <summary>
        /// string format representing UI 2012 bootstrap script.
        /// </summary>
        public static string Ui2012BootStrap = "<script type=\"text/javascript\" language=\"javascript\" defer=\"defer\" src=\"{0}/WebUI/Editors/SiteEdit/Views/Bootstrap/Bootstrap.aspx?mode=js\" id=\"tridion.siteedit\"></script>";

        /// <summary>
        /// Support function, checking if SE is enabled for the given item ID.
        /// </summary>
        /// <param name="item">the tridion item</param>
        /// <returns>true if the item belongs to a publication that has active SE in this web application, false otherwise</returns>
        public static bool IsSiteEditEnabled(IRepositoryLocal item)
        {
            string pubIdWithoutTcm = Convert.ToString(new TcmUri(item.Id).PublicationId);

            try
            {
                return SiteEditSettings.ContainsKey(pubIdWithoutTcm);
            }
            catch (Exception)
            {
                // todo: add logging (log.Error("Unable to get pubID from URI", ex))
                return false;
            }
        }

        /// <summary>
        /// Generates SiteEdit tag for given page.
        /// </summary>
        /// <param name="page">Page the tag belongs to.</param>
        /// <returns>string representing the JSON SiteEdit tag.</returns>
        public static string GenerateSiteEditPageTag(IPage page)
        {
            if (!SiteEditSettings.Enabled)
            {
                return string.Empty;
            }

            if (SiteEditSettings.Style == SiteEditStyle.SiteEdit2012)
            {
                string result = string.Format(Ui2012PageSeFormat, page.Id, string.Format("{0:s}", page.RevisionDate), page.PageTemplate.Id, string.Format("{0:s}", page.PageTemplate.RevisionDate));
                result += string.Format(Ui2012BootStrap, SiteEditSettings.TridionHostUrl);
                return result;
            }

            string pubIdWithoutTcm = Convert.ToString(new TcmUri(page.Id).PublicationId);

            if (SiteEditSettings.ContainsKey(pubIdWithoutTcm))
            {
                SiteEditSetting setting = SiteEditSettings[pubIdWithoutTcm];
                string usePageContext = string.IsNullOrEmpty(setting.PagePublication) ? page.OwningPublication.Id : setting.PagePublication;
                TcmUri pageContextUri = new TcmUri(usePageContext);
                if (setting.Enabled)
                {
                    return string.Format(PageSeFormat, page.Id, Convert.ToString(page.Version), pageContextUri.ItemId, setting.ComponentPublication, setting.PublishPublication);
                }
            }
            return string.Empty;
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
            return string.Format(Ui2012RegionSeFormat, title, schemaUri, templateUri, minOccurs, maxOccurs);
        }

        /// <summary>
        /// Generates a SiteEdit tag for a component presentation. Assumes that the component presentation is not query based.
        /// </summary>
        /// <param name="cp">the component presentation to mark.</param>
        /// <param name="region">The region the componentpresentation is to be shown in.</param>
        /// <returns>string representing the JSON SiteEdit tag.</returns>
        public static string GenerateSiteEditComponentTag(IComponentPresentation cp, string region)
        {
            return GenerateSiteEditComponentTag(cp, false, region);
        }

        /// <summary>
        /// Generates a SiteEdit tag for a componentpresentation. It also needs to know which region it's in (for component
        /// swapping) and the order of the page (for a true unique ID).
        /// </summary>
        /// <param name="cp">The componentpresentation to mark.</param>
        /// <param name="queryBased">indicates whether the component presentation is the result of a query (true), or if it is really part of the page (false)</param>
        /// <param name="region">The region the componentpresentation is to be shown in.</param>
        /// <returns>string representing the JSON SiteEdit tag.</returns>
        public static string GenerateSiteEditComponentTag(IComponentPresentation cp, bool queryBased, string region)
        {
            if (!SiteEditSettings.Enabled)
            {
                return string.Empty;
            }

            if (SiteEditSettings.Style == SiteEditStyle.SiteEdit2012)
            {
                // is query based tells us if the dcp was the result of a broker query and the component presentation is not embedded on the page
                string isQueryBased = queryBased ? ", \"IsQueryBased\" : true" : string.Empty;
                return string.Format(Ui2012ComponentSeFormat, cp.Component.Id, string.Format("{0:s}", cp.Component.RevisionDate), cp.ComponentTemplate.Id, string.Format("{0:s}", cp.ComponentTemplate.RevisionDate), cp.IsDynamic.ToString().ToLower(), isQueryBased);
            }

            string pubIdWithoutTcm = Convert.ToString(new TcmUri(cp.Component.Id).PublicationId);

            if (SiteEditSettings.ContainsKey(pubIdWithoutTcm))
            {
                SiteEditSetting setting = SiteEditSettings[pubIdWithoutTcm];
                if (setting.Enabled)
                {
                    int cpId;
                    if (queryBased)
                    {
                        // for query based CPs, we are responsible for making the ID unique
                        cpId = GetUniqueCpId();
                    }
                    else
                    {
                        cpId = cp.OrderOnPage;
                    }
                    return string.Format(ComponentSeFormat, cpId,
                        cp.Component.Id,
                        cp.ComponentTemplate.Id,
                        cp.Component.Version,
                        Convert.ToString(queryBased).ToLower(),
                        region);
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Function generates a fieldmarking for a single-value SiteEditable field based on field name and xpath. 
        /// For multi-value fields, please code the JSON yourself.
        /// </summary>
        /// <param name="fieldname">the name of the field</param>
        /// <param name="xpath">xpath</param>
        /// <returns>string representing the JSON SiteEdit tag.</returns>
        public static string GenerateSiteEditFieldMarkingWithXpath(string fieldname, string xpath)
        {
            if (SiteEditSettings.Style == SiteEditStyle.SiteEdit2012)
            {
                return string.Format(Ui2012FieldSeFormat, xpath);
            }
            return string.Format(FieldSeFormat, fieldname, "false", xpath);
        }

        #region obsolete members
        /**
         * Basic SiteEdit XPATH Prefix for simple fields (that is, non-multivalue and non-embedded).
         */
        public static string SIMPLE_SE_XPATH_PREFIX = "tcm:Content/custom:Content/custom:";
        public static string SINGLE_VALUE_SE_XPATH_Format = "tcm:Content/custom:{0}/custom:{1}";
        public static string MULTI_VALUE_SE_XPATH_Format = "tcm:Content/custom:{0}/custom:{1}[{2}]";

        /**
         * Function generates a fieldmarking for SiteEditable simple (non-multivalue and non-embedded) fields. For 
         * embedded fields, use the overloaded function with a better xpath. For multivalue fields, please code the JSON
         * yourself.
         * 
         * @param fieldname The Content Manager XML name of the field.
         * @return string representing the JSON SiteEdit tag.
         */
        [Obsolete("Please use GenerateSiteEditFieldTag instead")]
        public static string GenerateSiteEditFieldMarking(string fieldname)
        {
            return string.Format(FieldSeFormat, fieldname, "false", SIMPLE_SE_XPATH_PREFIX + fieldname);
        }

        [Obsolete("Please use GenerateSiteEditFieldTag instead")]
        public static string GenerateSiteEditFieldMarking(string fieldname, string schemaname)
        {
            return string.Format(FieldSeFormat,
                fieldname,
                "false",
                string.Format(SINGLE_VALUE_SE_XPATH_Format,
                    schemaname,
                    fieldname
                )
            );
        }

        /**
         * Generates a MV fieldmarking for specific instance of MV field.
         * 
         * @param fieldname Name of the field to mark
         * @param mvOrder Order of the MV instance
         * @return
         */
        [Obsolete("Please use GenerateSiteEditFieldTag instead")]
        public static string GenerateSiteEditFieldMarking(string fieldname, int mvOrder)
        {
            return string.Format(FieldSeFormat,
                fieldname,
                "true",
                string.Format(MULTI_VALUE_SE_XPATH_Format,
                    "Content",
                    fieldname,
                    mvOrder
                )
            );
        }

        /**
         * Generates a MV fieldmarking for specific instance of MV field.
         * 
         * @param fieldname Name of the field to mark
         * @param mvOrder Order of the MV instance
         * @return
         */
        [Obsolete("Please use GenerateSiteEditFieldTag instead")]
        public static string GenerateSiteEditFieldMarking(string fieldname, string schemaname, int mvOrder)
        {
            return string.Format(FieldSeFormat,
                fieldname,
                "true",
                string.Format(MULTI_VALUE_SE_XPATH_Format,
                    schemaname,
                    fieldname,
                    mvOrder
                )
            );
        }
        #endregion

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
            if (SiteEditSettings.Style == SiteEditStyle.SiteEdit2012)
            {
                return string.Format(Ui2012FieldSeFormat, field.XPath);
            }
            return string.Format(FieldSeFormat,
                XPath2Name(field.XPath),
                "false",
                field.XPath
            );
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

            if (SiteEditSettings.Style == SiteEditStyle.SiteEdit2012)
            {
                return string.Format(Ui2012FieldSeFormat, xpath);
            }
            return string.Format(FieldSeFormat,
                string.Format("{0}{1}", XPath2Name(field.XPath), mvOrder + 1),
                "true",
                xpath
            );
        }

        #region private members
        private static string XPath2Name(string xpath)
        {
            xpath = xpath.Replace("[", "").Replace("]", "");
            StringBuilder sb = new StringBuilder();
            string[] segments = xpath.Split('/');           
            foreach (string segment in segments.Skip<string>(1))
            {
                string[] segments2 = segment.Split(':');
                sb.Append(segments2[1]);
            }
            return sb.ToString();
        }

        private static int _idCounter = 100;
        
        private static int GetUniqueCpId()
        {
            lock (typeof(SiteEditService))
            {
                _idCounter++;
                if (_idCounter > 1000)
                {
                    _idCounter = 100;
                }
            }
            return _idCounter;
        }
        #endregion
    }
}
