using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    [SemanticEntity(Vocab = CoreVocabulary, EntityName = "FolderSchema", Prefix = "f")]
    public class Tsi2844PageModel : PageModel
    {
        public Tsi2844PageModel(string id) : base(id)
        {
        }

        [SemanticProperty("f:folderMetadataTextField")]
        public string FolderMetadataTextField { get; set; }

    }
}
