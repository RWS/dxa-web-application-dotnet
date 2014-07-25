using System.Collections.Generic;

namespace Sdl.Web.Common.Models.Interfaces
{
    public interface IRegion
    {
        Dictionary<string, string> RegionData { get; set; }
        string Module { get; set; }
        string Name { get; set; }
        List<object> Items { get; set; }
    }
}
