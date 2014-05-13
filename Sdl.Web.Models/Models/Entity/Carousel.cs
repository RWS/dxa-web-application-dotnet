using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Models
{
    public class Carousel : Entity
    {
        public List<Teaser> ItemListElements { get; set; }
    }
}
