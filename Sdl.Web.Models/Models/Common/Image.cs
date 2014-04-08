using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class Image : MediaItem
    {
        public string Url { get; set; }
        public string AlternateText { get; set; }
        public int FileSize { get; set; }
        //TODO: alternate formats
    }
}