using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc
{
    public class Localization
    {
        public int LocalizationId { get; set;}
        public string Domain { get; set; }
        public string Port { get; set; }
        public string Path { get; set; }
        public string Protocol { get; set; }
        public string Culture { get; set; }
        public string GetBaseUrl() 
        {
            return String.Format("{0}://{1}{2}{3}", Protocol, Domain, Port, Path);
        }
    }
}
