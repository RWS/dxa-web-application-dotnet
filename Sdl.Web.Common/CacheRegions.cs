using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common
{
    /// <summary>
    /// Constants for the names of Cache Regions used by the DXA Framework.
    /// </summary>
    public static class CacheRegions
    {
        public const string PageModel = "PageModel";
        public const string IncludePageModel = "IncludePageModel";
        public const string EntityModel = "EntityModel";
        public const string StaticNavigation = "Navigation_Static";
        public const string DynamicNavigation = "Navigation_Dynamic";
        public const string NavigationTaxonomy = "NavTaxonomy";
    }
}
