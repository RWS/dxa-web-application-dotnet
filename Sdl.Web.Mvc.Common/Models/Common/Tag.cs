using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models
{
    public class Tag
    {
        //Text to display
        public string DisplayText { get; set; }
        //Unique identifier for the tag (within the given domain)
        public string Key { get; set; }
        //The domain/category/taxonomy identifier for the tag
        public string TagCategory { get; set; }
    }
}
