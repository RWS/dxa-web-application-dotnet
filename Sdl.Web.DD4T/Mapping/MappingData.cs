using DD4T.ContentModel;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.DD4T.Mapping
{
    public class MappingData
    {
        public Type TargetType { get; set; }
        public IFieldSet Content { get; set; }
        public IFieldSet Meta { get; set; }
        public Dictionary<string, string> Vocabularies { get; set; }
        public SemanticSchema SemanticSchema { get; set; }
        public ILookup<string, string> EntityNames { get; set; }
        public string ParentDefaultPrefix { get; set; }
        public int EmbedLevel { get; set; }
        public IComponent SourceEntity { get; set; }
    }
}
