using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Models
{
    public class FeedItem : EntityModel
    {
        public string Title { get; set; }

        public RichText Summary { get; set; }

        public Link Link { get; set; }

        public DateTime? Date { get; set; }
    }
}
