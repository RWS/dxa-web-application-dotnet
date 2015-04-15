using System.Collections.Generic;
namespace Sdl.Web.Common.Models
{
    public class Configuration : EntityBase
    {
        [SemanticProperty("_all")]
        public Dictionary<string, string> Settings { get; set; }
        public Configuration()
        {
            Settings = new Dictionary<string, string>();
        }
    }
}