using Sdl.Tridion.Api.Client.ContentModel;
using Sdl.Tridion.Api.Client.Utils;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.ApiClient
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
