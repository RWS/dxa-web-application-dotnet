using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [Obsolete("Deprecated in DXA 1.1. Use class PageModel instead.")]
    public interface IPage
    {
        Dictionary<string, string> PageData { get; set; }
        // TODO: Create RegionCollection which acts as IEnumerable<Region> and also has indexer 
        Dictionary<string, IRegion> Regions { get; set; }
        string Id { get; set; }
        MvcData AppData { get; set; }
    }
}
