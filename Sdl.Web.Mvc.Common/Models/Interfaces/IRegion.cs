using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models.Interfaces
{
    public interface IRegion
    {
        Dictionary<string, string> RegionData { get; set; }
        string Module { get; set; }
        string Name { get; set; }
        List<object> Items { get; set; }
    }
}
