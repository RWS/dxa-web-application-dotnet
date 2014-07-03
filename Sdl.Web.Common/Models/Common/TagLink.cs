using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models
{
    public class TagLink : EntityBase
    {
        [SemanticProperty(PropertyName = "internalLink")]
        [SemanticProperty(PropertyName = "externalLink")]
        public string Url { get; set; }
        public Tag Tag { get; set; }
    }
}
