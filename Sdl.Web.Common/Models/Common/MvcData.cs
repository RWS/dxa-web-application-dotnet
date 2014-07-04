using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models
{
    public class MvcData
    {
        public string ControllerName { get; set; }
        public string ControllerAreaName { get; set; }
        public string ActionName { get; set; }
        public string ViewName { get; set; }
        public string AreaName { get; set; }
        public Dictionary<string, string> RouteValues { get; set; }
    }
}
