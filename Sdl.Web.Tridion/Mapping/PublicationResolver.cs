using System;
using DD4T.ContentModel.Contracts.Resolvers;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    public class PublicationResolver : IPublicationResolver
    {       
        public int ResolvePublicationId()
        {
            return Convert.ToInt32(WebRequestContext.Localization.Id);
        }
    }
}
