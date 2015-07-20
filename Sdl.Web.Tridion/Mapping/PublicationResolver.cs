using System;
using DD4T.ContentModel.Contracts.Resolvers;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    public class PublicationResolver : IPublicationResolver
    {
        public int ResolvePublicationId()
        {
            try
            {
                return Int32.Parse(WebRequestContext.Localization.LocalizationId);
            }
            catch (NullReferenceException e)
            {
                // catching this error so it is not in the way when debugging
                Log.Debug("WebRequestContext.Localization is null{0}", e.StackTrace);

                // TODO: should it be allowed that WebRequestContext.Localization is null, and can we safely return 0 in that case?
                return 0;
            }
        }
    }
}
