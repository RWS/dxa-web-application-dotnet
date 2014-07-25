using System.Collections.Generic;

namespace Sdl.Web.Common.Models.Interfaces
{
    public interface IPage
    {
        Dictionary<string, string> PageData { get; set; }
        Dictionary<string, IRegion> Regions { get; set; }
        string Id { get; set; }
    }
}
