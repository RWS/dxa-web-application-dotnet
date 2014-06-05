using System;

namespace Sdl.Web.Mvc.Models
{
    public class YouTubeVideo : MediaItem
    {
        public string Url { get; set; }
        public string YouTubeId { get; set; }
        public int FileSize { get; set; }
    }
}
