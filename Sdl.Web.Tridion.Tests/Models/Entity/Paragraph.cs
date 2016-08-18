using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    public class Paragraph : EntityModel
    {
        public string Subheading { get; set; }
        public RichText Content { get; set; }
        public MediaItem Media { get; set; }
        public string Caption { get; set; }
    }
}