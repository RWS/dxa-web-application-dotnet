using Sdl.Web.Common;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.PublicContentApi.Utils;

namespace Sdl.Web.Tridion.PCAClient
{
    public static class Extensions
    {
        public static ContentNamespace Namespace(this ILocalization localization)
            => CmUri.NamespaceIdentiferToId(localization.CmUriScheme);

        public static int PublicationId(this ILocalization localization)
        {
            int pubId;
            if (!int.TryParse(localization.Id, out pubId))
                throw new DxaItemNotFoundException($"Invalid publication id '{localization.Id}' stored in localization.");
            return pubId;
        }
    }
}
