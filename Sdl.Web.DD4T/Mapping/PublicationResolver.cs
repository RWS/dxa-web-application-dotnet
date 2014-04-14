using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Utils;
using Sdl.Web.Tridion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sdl.Web.DD4T
{
    /// <summary>
    /// Tries to read url -> host mappings from cd_link_conf.xml, and otherwise falls back on the standard setting in web.config
    /// </summary>
    public class PublicationResolver : IPublicationResolver
    {
        public int ResolvePublicationId()
        {
            int pubid = TridionConfig.GetPublicationIdFromUrl(HttpContext.Current.Request.Url);
            if (pubid==0)
            {
                pubid = ConfigurationHelper.PublicationId;
            }
            return pubid;
        }
    }
}
