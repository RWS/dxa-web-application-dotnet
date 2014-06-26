using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Mvc.Models
{
    public class MediaItem : Entity
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public string MimeType { get; set; }
    }
}