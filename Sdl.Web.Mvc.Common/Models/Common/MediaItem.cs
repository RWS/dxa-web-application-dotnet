using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sdl.Web.Models
{
    public class MediaItem : EntityBase
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
    }
}