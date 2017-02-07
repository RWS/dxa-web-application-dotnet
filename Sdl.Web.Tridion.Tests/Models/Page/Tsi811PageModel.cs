using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity("TSI811PageMetadataSchema")]
    public class Tsi811PageModel : PageModel
    {
        public Tsi811PageModel(string id) : base(id)
        {
        }

        public Tsi811TestKeyword PageKeyword { get; set; }
    }
}
