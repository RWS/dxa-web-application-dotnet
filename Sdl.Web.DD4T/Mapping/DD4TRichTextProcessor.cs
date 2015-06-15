using System;
using System.Linq;
using System.Web;
using System.Xml;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DD4T.Utils;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Tridion.Linking;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Default (DD4T-based) Rich Text Processor implementation
    /// </summary>
    public class DD4TRichTextProcessor : IRichTextProcessor
    {
        private readonly IComponentFactory _componentFactory;

        public DD4TRichTextProcessor(IComponentFactory componentFactory)
        {
            _componentFactory = componentFactory;
        }

        #region IRichTextProcessor Members

        /// <summary>
        /// Processes rich text (XHTML) content.
        /// </summary>
        /// <param name="xhtml">The rich text content (XHTML fragment) to be processed.</param>
        /// <returns>The processed rich text content.</returns>
        /// <remarks>
        /// Typical rich text processing tasks: 
        /// <list type="bullet">
        ///     <item>Convert XHTML to plain HTML</item>
        ///     <item>Resolve inline links</item>
        /// </list>
        /// </remarks>
        public string ProcessRichText(string xhtml)
        {
            try
            {
                XmlDocument xhtmlDoc = new XmlDocument();
                xhtmlDoc.LoadXml(String.Format("<xhtml>{0}</xhtml>", xhtml));
                return ResolveRichText(xhtmlDoc);
            }
            catch (XmlException ex)
            {
                Log.Warn("An error occurred parsing XHTML fragment; rich text processing is skipped: {0}\nXHTML fragment:\n{1}", ex.Message, xhtml);
                return xhtml;
            }
        }

        #endregion

        private string ResolveRichText(XmlDocument doc)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");
            nsmgr.AddNamespace("xlink", "http://www.w3.org/1999/xlink");

            // resolve links which haven't been resolved
            ILinkResolver linkResolver = SiteConfiguration.LinkResolver;
            foreach (XmlNode link in doc.SelectNodes("//a[@xlink:href[starts-with(string(.),'tcm:')]][@href='' or not(@href)]", nsmgr))
            {
                // does this link already have a resolved href?
                string linkUrl = link.Attributes["href"].IfNotNull(attr => attr.Value);
                if (String.IsNullOrEmpty(linkUrl))
                {
                    // DD4T BinaryPublisher resolves these links and adds a src rather than a href, let's try that
                    linkUrl = link.Attributes["src"].IfNotNull(attr => attr.Value);
                    // lets remove that invalid attribute directly 
                    link.Attributes.Remove(link.Attributes["src"]);
                }
                if (String.IsNullOrEmpty(linkUrl))
                {
                    // assume dynamic component link and try to resolve
                    linkUrl = link.Attributes["xlink:href"].IfNotNull(attr => linkResolver.ResolveLink(attr.Value));                    
                }                
                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // add href
                    XmlAttribute href = doc.CreateAttribute("href");
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

            // resolve youtube videos
            foreach (XmlNode youtube in doc.SelectNodes("//img[@data-youTubeId]", nsmgr))
            {
                string uri = youtube.Attributes["xlink:href"].IfNotNull(attr => attr.Value);
                string id = youtube.Attributes["data-youTubeId"].IfNotNull(attr => attr.Value);
                string headline = youtube.Attributes["data-headline"].IfNotNull(attr => attr.Value);
                string src = youtube.Attributes["src"].IfNotNull(attr => attr.Value);
                if (!string.IsNullOrEmpty(uri))
                {
                    // call media helper for youtube video like is done in the view 
                    string element;
                    if (SiteConfiguration.MediaHelper.ShowVideoPlaceholders)
                    {
                        // we have a placeholder image
                        string placeholderImgUrl = SiteConfiguration.MediaHelper.GetResponsiveImageUrl(src, 0, "100%");
                        element = HtmlHelperExtensions.GetYouTubePlaceholder(id, placeholderImgUrl, headline, null, "span", true);
                    }
                    else
                    {
                        element = HtmlHelperExtensions.GetYouTubeEmbed(id);                        
                    }

                    // convert the element (which is a string) to an xmlnode 
                    XmlDocument temp = new XmlDocument();
                    temp.LoadXml(element);
                    temp.DocumentElement.SetAttribute("xmlns", "http://www.w3.org/1999/xhtml");
                    XmlNode video = doc.ImportNode(temp.DocumentElement, true);

                    // replace youtube element with actual html
                    youtube.ParentNode.ReplaceChild(video, youtube);
                }
            }

            return doc.DocumentElement.InnerXml;
        }

        private void ApplyHashIfApplicable(XmlNode link)
        {
            string target = link.Attributes["target"].IfNotNull(attr => attr.Value.ToLower());

            if("anchored" == target) 
            {
                string href = link.Attributes["href"].Value;

                bool samePage = string.Equals(href,
                    HttpContext.Current.Request.Url.AbsolutePath,
                    StringComparison.OrdinalIgnoreCase
                );
                
                string hash = GetLinkName(link).IfNotNull(s => '#' + s.Replace(" ", "_").ToLower());
                link.Attributes["href"].Value = (!samePage ? href : string.Empty) + hash;
                link.Attributes["target"].Value = !samePage ? "_top" : string.Empty;
            }
        }

        private string GetLinkName(XmlNode link)
        {
            string componentUri = link.Attributes["xlink:href"].IfNotNull(attr => attr.Value);
            
            return _componentFactory.GetComponent(componentUri).IfNotNull(c => c.Title)
                ?? link.Attributes["title"].IfNotNull(attr => attr.Value);
        }

    }   
}