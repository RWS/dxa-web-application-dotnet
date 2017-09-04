using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace DD4T.RestService.WebApi.Helpers
{
    public static class Extensions
    {
       
        public static string GetUrl(this string url, string extension)
        {
            if (url.EndsWith("/"))
                url = url.Remove(url.Length - 1);

            return string.Format("/{0}.{1}", url, extension);  
        }

        public static string ToPageTcmUri(this int id, int publicationId)
        {
            return string.Format("tcm:{0}-{1}-64", publicationId, id);
        }

        public static string ToComponentTcmUri(this int id, int publicationId)
        {
             return string.Format("tcm:{0}-{1}-16", publicationId, id);
        }

        public static string ToComponentTemplateTcmUri(this int id, int publicationId)
        {
            return string.Format("tcm:{0}-{1}-32", publicationId, id);
        }

        public static string ToCategoryTcmUri(this int id, int publicationId)
        {
            return string.Format("tcm:{0}-{1}-512", publicationId, id);
        }
    }
}