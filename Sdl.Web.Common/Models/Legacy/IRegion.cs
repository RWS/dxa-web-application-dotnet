using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [Obsolete("Deprecated in DXA 1.1. Use class RegionModel instead.")]
    public interface IRegion
    {
        Dictionary<string, string> RegionData { get; set; }
        string Module { get; set; }
        string Name { get; set; }
        List<object> Items { get; set; }
        MvcData AppData { get; set; }
    }
}
