using System;

namespace Sdl.Web.Common.Configuration
{
    public class Localization
    {
        public string LocalizationId { get; set;}
        public string Domain { get; set; }
        public string Port { get; set; }
        public string Path { get; set; }
        public string Protocol { get; set; }
        public string Culture { get; set; }
        public string GetBaseUrl() 
        {
            return String.Format("{0}://{1}{2}{3}", Protocol, Domain, String.IsNullOrEmpty(Port) ? Port : ":" + Port, String.IsNullOrEmpty(Path) || Path.StartsWith("/") ? Path : "/" + Path);
        }
    }
}
