using System;

namespace Sdl.Web.Mvc.Models
{
    public class YouTubeVideo : MediaItem
    {
        public YouTubeVideo()
        {
            InstanceId = Guid.NewGuid();
        }
        public Guid InstanceId { get; private set; }

        // url of image, can be used as alternative or first frame overlay
        public string Url { get; set; }
        public string YouTubeId { get; set; }
        public int FileSize { get; set; }
        public string Embed
        {
            get
            {
                //return String.Format("<iframe id=\"{3}\" width=\"{1}\" height=\"{2}\" src=\"//www.youtube.com/embed/{0}\" allowfullscreen></iframe>", YouTubeId, Width, Height, InstanceId);
                return String.Format("<iframe id=\"video-{1}\" src=\"//www.youtube.com/embed/{0}?version=3&enablejsapi=1 \" allowfullscreen></iframe>", YouTubeId, InstanceId);
            }
        }

        // TODO determine correct width and height or allow to be set
        public int Width { get { return 640; } }
        public int Height { get { return 390; } }
    }
}