namespace Sdl.Web.Common.Models
{
    public class Paragraph : EntityModel
    {
        public string Subheading { get; set; }
        public RichText Content { get; set; }
        public MediaItem Media { get; set; }
        public string Caption { get; set; }
    }
}