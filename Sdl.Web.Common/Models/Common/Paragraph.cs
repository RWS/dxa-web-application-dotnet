namespace Sdl.Web.Common.Models
{
    public class Paragraph : EntityBase
    {
        public string Subheading { get; set; }
        public string Content { get; set; }
        public MediaItem Media { get; set; }
        public string Caption { get; set; }
    }
}