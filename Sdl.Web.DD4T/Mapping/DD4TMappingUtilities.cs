using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Models;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Utility methods for DD4T mapping
    /// </summary>
    internal static class DD4TMappingUtilities
    {
        /// <summary>
        /// Determine MVC data such as view, controller and area name from a Page
        /// </summary>
        /// <param name="page">The DD4T Page object</param>
        /// <returns>MVC data</returns>
        internal static MvcData ResolveMvcData(IPage page)
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
        public static MvcData ResolveMvcData(IComponentPresentation cp)
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
