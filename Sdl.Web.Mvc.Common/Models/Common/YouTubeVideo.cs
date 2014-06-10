using System;

namespace Sdl.Web.Mvc.Models
{
    public class YouTubeVideo : MediaItem
    {
        // url of image, can be used as alternative or first frame overlay
        public string Url { get; set; }
        public string YouTubeId { get; set; }
        public int FileSize { get; set; }
        public string Embed
        {
            get
            {
                //return String.Format("<iframe width=\"{1}\" height=\"{2}\" src=\"//www.youtube.com/embed/{0}\"></iframe>", YouTubeId, Width, Height);
                return String.Format("<iframe src=\"//www.youtube.com/embed/{0}\"></iframe>", YouTubeId);
            }
        }
        // TODO determine correct width and height or allow to be set
        public int Width { get { return 640; } }
        public int Height { get { return 390; } }
    }
}