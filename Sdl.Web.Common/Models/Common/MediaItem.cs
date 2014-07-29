namespace Sdl.Web.Common.Models
{
    public class MediaItem : EntityBase
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public int FileSize { get; set; }
        public string MimeType { get; set; }
    }
}