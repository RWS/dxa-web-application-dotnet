using System;
using DD4T.ContentModel.Factories;
using DD4T.Factories;

namespace Sdl.Web.DD4T
{
    public class ExtensionlessLinkFactory : LinkFactory, ILinkFactory
    {
        [Obsolete]
        public string ResolveExtensionlessLink(string componentUri)
        {
            return ((ILinkFactory)this).ResolveLink(componentUri);
        }

        protected virtual string RemoveExtension(string url)
        {
            if (url != null)
            {
                var pos = url.LastIndexOf(".");
                if (pos > url.LastIndexOf("/"))
                {
                    url = url.Substring(0, pos);
                }
            }
            return url;
        }
        
        string ILinkFactory.ResolveLink(string sourcePageUri, string componentUri, string excludeComponentTemplateUri)
        {
            return  RemoveExtension(base.ResolveLink(sourcePageUri, componentUri, excludeComponentTemplateUri));
        }
        
        string ILinkFactory.ResolveLink(string componentUri)
        {
            return  RemoveExtension(base.ResolveLink(componentUri));
        }
    }
}