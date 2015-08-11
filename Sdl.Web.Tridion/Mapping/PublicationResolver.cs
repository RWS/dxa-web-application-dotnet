using System;
using DD4T.ContentModel.Contracts.Resolvers;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    internal class PublicationResolver : IPublicationResolver
    {
        private readonly int _publicationId;

        public PublicationResolver(Localization localization)
        {
            _publicationId = Convert.ToInt32(localization.LocalizationId);
        }

        public int ResolvePublicationId()
        {
            return _publicationId;
        }
    }
}
