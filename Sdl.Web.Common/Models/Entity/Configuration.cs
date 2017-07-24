using System;
using System.Collections.Generic;
namespace Sdl.Web.Common.Models
{
    [Serializable]
    [DxaNoOutputCache]
    public class Configuration : EntityModel
    {
        [SemanticProperty("_all")]
        public Dictionary<string, string> Settings { get; set; }
        public Configuration()
        {
            Settings = new Dictionary<string, string>();
        }
    }
}