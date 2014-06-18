using System;
using System.Linq;
using System.Web;
using System.Xml;
using DD4T.ContentModel.Factories;

namespace SDL.Web.Helpers
{
    public class RichTextHelper
    {
        readonly IComponentFactory ComponentFactory;
        readonly ILinkFactory ComponentLinkProvider;

        public RichTextHelper(ILinkFactory componentLinkProvider, IComponentFactory componentFactory)
        {
            ComponentLinkProvider = componentLinkProvider;
            ComponentFactory = componentFactory;
        }

        public string ResolveRichText(string xml)
        {
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(string.Format("<xhtml>{0}</xhtml>", xml));
                return ResolveRichText(doc);
            }
            catch (XmlException)
            {
                return xml;
            }
        }
        
        /// <summary>
        /// Extension method on String to resolve rich text. 
        /// 
        /// Does the following:
        ///  - strips XML artifacts
        ///  - resolve links
        ///  - post-process "anchored" links to include #hash
        /// </summary>
        public string ResolveRichText(XmlDocument doc)
        {
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");

            // resolve links which haven't been resolved
            foreach (XmlNode link in doc.SelectNodes("//xhtml:a[@xlink:href[starts-with(string(.),'tcm:')]][@xhtml:href='' or not(@xhtml:href)]", nsmgr))
            {
                var linkUrl =
                    link.Attributes["href"].IfNotNull(attr => attr.Value)
                    ?? link.Attributes["xlink:href"].IfNotNull(attr => ComponentLinkProvider.ResolveLink(attr.Value));
                
                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // add href
                    var href = doc.CreateAttribute("xhtml:href");
                    href.Value = linkUrl;
                    link.Attributes.Append(href);

                    ApplyHashIfApplicable(link);

                    // remove all xlink attributes
                    foreach (XmlAttribute xlinkAttr in link.SelectNodes("//@xlink:*", nsmgr))
                    {
                        link.Attributes.Remove(xlinkAttr);
                    }
                }
                else
                {
                    // copy child nodes of link so we keep them
                    link.ChildNodes.Cast<XmlNode>()
                        .Select(link.RemoveChild)
                        .ToList()
                        .ForEach(child => 
                        {
                            link.ParentNode.InsertBefore(child, link);
                        });
                    // remove link node
                    link.ParentNode.RemoveChild(link);
                }
            }

            return doc.DocumentElement.InnerXml;
        }

        void ApplyHashIfApplicable(XmlNode link)
        {
            var target = link.Attributes["target"].IfNotNull(attr => attr.Value.ToLower());

            if("anchored" == target) {

                var href = link.Attributes["xhtml:href"].Value;

                var samePage = string.Equals(href,
                    HttpContext.Current.Request.Url.AbsolutePath,
                    StringComparison.OrdinalIgnoreCase
                );
                
                var hash = GetLinkName(link).IfNotNull(s => '#' + s.Replace(" ", "_").ToLower());
                link.Attributes["xhtml:href"].Value = (!samePage ? href : string.Empty) + hash;
                link.Attributes["target"].Value = !samePage ? "_top" : string.Empty;
            }
        }

        string GetLinkName(XmlNode link)
        {
            var componentUri = link.Attributes["xlink:href"].IfNotNull(attr => attr.Value);
            
            return this.ComponentFactory.GetComponent(componentUri).IfNotNull(c => c.Title)
                ?? link.Attributes["title"].IfNotNull(attr => attr.Value);
        }
    }
}