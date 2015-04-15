using System;
using Tridion.ContentDelivery.Web.Linking;

namespace Sdl.Web.Tridion.Linking
{
    public static class TridionHelper
    {
        public static string ResolveLink(string uri, int localizationId = 0, bool isBinary = false)
        {
            if (uri.StartsWith("tcm:"))
            {
                uri = uri.Substring(4);
            }
            var bits = uri.Split('-');
            if (bits.Length > 2)
            {
                switch(bits[2])
                {
                    case "64":
                        return ResolvePageLink(uri, localizationId);
                    case "16":
                        return isBinary ? ResolveBinaryLink(uri, localizationId) : ResolveComponentLink(uri, localizationId);
                    default:
                        return null;
                }
            }

            return isBinary ? ResolveBinaryLink(uri, localizationId) : ResolveComponentLink(uri, localizationId);
        }

        private static string ResolveComponentLink(string uri, int localizationId = 0)
        {
            //TODO should we have a single (static) link object?
            var linker = new ComponentLink(localizationId==0 ? GetPublicationIdFromUri(uri) : localizationId);
            var link = linker.GetLink(GetItemIdFromUri(uri));
            return link.IsResolved ? link.Url : null;
        }

        private static string ResolveBinaryLink(string uri, int localizationId = 0)
        {
            //TODO should we have a single (static) link object?
            var linker = new BinaryLink(localizationId == 0 ? GetPublicationIdFromUri(uri) : localizationId);
            var link = linker.GetLink(uri.StartsWith("tcm:") ? uri : "tcm:" + uri,null,null,null,false);
            return link.IsResolved ? link.Url : null;
        }

        private static string ResolvePageLink(string uri, int localizationId = 0)
        {
            //TODO should we have a single (static) link object?
            var linker = new PageLink(localizationId == 0 ? GetPublicationIdFromUri(uri) : localizationId);
            var link = linker.GetLink(GetItemIdFromUri(uri));
            return link.IsResolved ? link.Url : null;
        }

        public static int GetPublicationIdFromUri(string uri)
        {
            if (uri.StartsWith("tcm:"))
            {
                uri = uri.Substring(4);
            }
            int res;
            var bits = uri.Split('-');
            if (Int32.TryParse(bits[0], out res))
            {
                return res;
            }

            throw new Exception("Invalid URI: " + uri);
        }

        public static int GetItemIdFromUri(string uri)
        {
            var bits = uri.Split('-');
            int res;
            if (Int32.TryParse(bits[1], out res))
            {
                return res;
            }

            throw new Exception("Invalid URI: " + uri);
        }

        public static int GetItemTypeFromUri(string uri)
        {
            var bits = uri.Split('-');
            if (bits.Length > 2)
            {
                int res;
                if (Int32.TryParse(bits[2], out res))
                {
                    return res;
                }

                throw new Exception("Invalid URI: " + uri);
            }
            {
                return 16;
            }
        }
    }
}
