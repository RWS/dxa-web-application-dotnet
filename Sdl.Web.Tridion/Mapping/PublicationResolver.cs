using System;
using DD4T.ContentModel.Contracts.Resolvers;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    public class PublicationResolver : IPublicationResolver
    {
        private readonly int _publicationId;

        public PublicationResolver(Localization localization)
        {
            _publicationId = Convert.ToInt32(localization.Id);
        }

        public int ResolvePublicationId()
        {
            return _publicationId == 0 ? Convert.ToInt32(WebRequestContext.Localization.Id) : _publicationId;
        }
    }
}
