using DD4T.Factories;
using System.Xml;
using System.Web.Mvc;
using DD4T.ContentModel.Factories;
using System;
using System.Web;

namespace DD4T.Mvc.Html
{
    [Obsolete("Use 'DD4T.Core.Contracts.Resolvers.IRichTextResolver', with default binding 'DD4T.Utils.Resolver.DefaultRichTextResolver'. Binding is done if you're using one off the provided DD4T DI Containers.")]
    public static class RichTextHelper 
    {
        /// <summary>
        /// xhtml namespace uri
        /// </summary>
        private const string XhtmlNamespaceUri = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// xlink namespace uri
        /// </summary>
        private const string XlinkNamespaceUri = "http://www.w3.org/1999/xlink";

        private static ILinkFactory _linkFactory;

        static RichTextHelper()
        {
            //this is Anti-Pattern, there is no other way to inject dependencies into this class.
            //This helper should not be used in views, this logic should get executed by the controller.
            var linkFactory = DependencyResolver.Current.GetService<ILinkFactory>();
            _linkFactory = linkFactory;
        }

        public static MvcHtmlString ResolveRichText(this string value)
        {
            XmlDocument doc = new XmlDocument();
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);

            nsmgr.AddNamespace("xhtml", XhtmlNamespaceUri);
            nsmgr.AddNamespace("xlink", XlinkNamespaceUri);
            var encodeValue = HttpUtility.HtmlEncode(value);
            doc.LoadXml(string.Format("<xhtmlroot>{0}</xhtmlroot>", encodeValue));
            // resolve links which haven't been resolved
            foreach (XmlNode link in doc.SelectNodes("//xhtml:a[@xlink:href[starts-with(string(.),'tcm:')]][@xhtml:href='' or not(@xhtml:href)]", nsmgr))
            {
                string tcmuri = link.Attributes["xlink:href"].Value;
                string linkUrl = _linkFactory.ResolveLink(tcmuri);

                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // linkUrl = HttpHelper.AdjustUrlToContext(linkUrl);
                    // add href
                    XmlAttribute href = doc.CreateAttribute("xhtml:href");
                    href.Value = linkUrl;
                    link.Attributes.Append(href);

                    // remove all xlink attributes
                    foreach (XmlAttribute xlinkAttr in link.SelectNodes("//@xlink:*", nsmgr))
                        link.Attributes.Remove(xlinkAttr);
                }
                else
                {
                    //Try and get a binary url for the tcmUri if one exists
                    IBinaryFactory bf = DependencyResolver.Current.GetService<IBinaryFactory>();
                    linkUrl = bf.GetUrlForUri(tcmuri);
                    if (!string.IsNullOrEmpty(linkUrl))
                    {
                        // linkUrl = HttpHelper.AdjustUrlToContext(linkUrl);
                        // add href
                        XmlAttribute href = doc.CreateAttribute("xhtml:href");
                        href.Value = linkUrl;
                        link.Attributes.Append(href);

                        // remove all xlink attributes
                        foreach (XmlAttribute xlinkAttr in link.SelectNodes("//@xlink:*", nsmgr))
                            link.Attributes.Remove(xlinkAttr);
                    }
                    else
                    {
                        // copy child nodes of link so we keep them
                        foreach (XmlNode child in link.ChildNodes)
                            link.ParentNode.InsertBefore(child.CloneNode(true), link);

                        // remove link node
                        link.ParentNode.RemoveChild(link);
                    }
                }
            }

            // remove any additional xlink attribute
            foreach (XmlNode node in doc.SelectNodes("//*[@xlink:*]", nsmgr))
            {
                foreach (XmlAttribute attr in node.SelectNodes("//@xlink:*", nsmgr))
                    node.Attributes.Remove(attr);
            }

            // add application context path to images
            foreach (XmlElement img in doc.SelectNodes("//*[@src]", nsmgr))
            {
                //if (img.GetAttributeNode("src") != null)
                //    img.Attributes["src"].Value = HttpHelper.AdjustUrlToContext(img.Attributes["src"].Value);
            }

            // fix empty anchors by placing the id value as a text node and adding a style attribute with position:absolute and visibility:hidden so the value won't show up
            foreach (XmlElement anchor in doc.SelectNodes("//xhtml:a[not(node())]", nsmgr))
            {
                XmlAttribute style = doc.CreateAttribute("style");
                style.Value = "position:absolute;visibility:hidden;";
                anchor.Attributes.Append(style);
                anchor.InnerText = anchor.Attributes["id"] != null ? anchor.Attributes["id"].Value : "empty";
            }

            return new MvcHtmlString(HttpUtility.HtmlDecode(RemoveNamespaceReferences(doc.DocumentElement.InnerXml)));
        }

        /// <summary>
        /// removes unwanted namespace references (like xhtml and xlink) from the html
        /// </summary>
        /// <param name="html">html as a string</param>
        /// <returns>html as a string without namespace references</returns>
        private static string RemoveNamespaceReferences(string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                html = html.Replace("xmlns=\"\"", "");
                html = html.Replace(string.Format("xmlns=\"{0}\"", XhtmlNamespaceUri), "");
                html = html.Replace(string.Format("xmlns:xhtml=\"{0}\"", XhtmlNamespaceUri), "");
                html = html.Replace(string.Format("xmlns:xlink=\"{0}\"", XlinkNamespaceUri), "");
            }

            return html;
        }
    }
}
