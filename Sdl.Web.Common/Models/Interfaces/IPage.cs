using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public interface IPage
    {
        Dictionary<string, string> PageData { get; set; }
        // TODO: Create RegionCollection which acts as IEnumerable<Region> and also has indexer 
        Dictionary<string, IRegion> Regions { get; set; }
        string Id { get; set; }
        MvcData AppData { get; set; }
    }
}
