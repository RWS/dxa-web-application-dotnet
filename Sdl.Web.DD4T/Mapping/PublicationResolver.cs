using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Utils;
using Sdl.Web.Mvc;
using Sdl.Web.Tridion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sdl.Web.DD4T
{
    public class PublicationResolver : IPublicationResolver
    {
        public int ResolvePublicationId()
        {
            return WebRequestContext.Localization.LocalizationId;
        }
    }
}
