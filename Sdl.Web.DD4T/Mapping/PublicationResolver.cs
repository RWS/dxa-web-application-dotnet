using System;
using DD4T.ContentModel.Contracts.Resolvers;
using Sdl.Web.Mvc;

namespace Sdl.Web.DD4T.Mapping
{
    public class PublicationResolver : IPublicationResolver
    {
        public int ResolvePublicationId()
        {
            return Int32.Parse(WebRequestContext.Localization.LocalizationId);
        }
    }
}
