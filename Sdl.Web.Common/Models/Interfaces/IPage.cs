using Sdl.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models.Interfaces
{
    public interface IPage
    {
        Dictionary<string, string> PageData { get; set; }
        Dictionary<string, IRegion> Regions { get; set; }
        string Id { get; set; }
    }
}
