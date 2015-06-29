using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
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
        private const string _embeddedEntityProcessingInstructionName = "EmbeddedEntity";
        private static readonly Regex _embeddedEntityProcessingInstructionRegex = new Regex(@"<\?EmbeddedEntity\s\?>", RegexOptions.Compiled);
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
        public RichText ProcessRichText(string xhtml)
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
                return new RichText(xhtml);
            }
        }

        #endregion

        private RichText ResolveRichText(XmlDocument doc)
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

            // Resolve embedded YouTube videos
            List<EntityModel> embeddedEntities = new List<EntityModel>();
            foreach (XmlElement youTubeImgElement in doc.SelectNodes("//img[@data-youTubeId][@xlink:href]", nsmgr))
            {
                YouTubeVideo youTubeVideo = new YouTubeVideo
                {
                    Id =  DD4TModelBuilder.GetDxaIdentifierFromTcmUri(youTubeImgElement.GetAttribute("xlink:href")), 
                    Url = youTubeImgElement.GetAttribute("src"),
                    YouTubeId = youTubeImgElement.GetAttribute("data-youTubeId"),
                    Headline = youTubeImgElement.GetAttribute("data-headline"),
                    IsEmbedded = true,
                    MvcData = new MvcData("Core:Entity:YouTubeVideo")
                };
                embeddedEntities.Add(youTubeVideo);

                // Replace YouTube img element with marker XML processing instruction 
                youTubeImgElement.ParentNode.ReplaceChild(
                    doc.CreateProcessingInstruction(_embeddedEntityProcessingInstructionName, string.Empty), 
                    youTubeImgElement
                    );
            }

            // Split the XHTML into fragments based on marker XML processing instructions.
            string xhtml = doc.DocumentElement.InnerXml;
            IList<IRichTextFragment> richTextFragments = new List<IRichTextFragment>();
            int lastFragmentIndex = 0;
            int i = 0;
            foreach (Match embeddedEntityMatch in _embeddedEntityProcessingInstructionRegex.Matches(xhtml))
            {
                int embeddedEntityIndex = embeddedEntityMatch.Index;
                if (embeddedEntityIndex > lastFragmentIndex)
                {
                    richTextFragments.Add(new RichTextFragment(xhtml.Substring(lastFragmentIndex, embeddedEntityIndex - lastFragmentIndex)));
                }
                richTextFragments.Add(embeddedEntities[i++]);
                lastFragmentIndex = embeddedEntityIndex + embeddedEntityMatch.Length;
            }
            if (lastFragmentIndex < xhtml.Length)
            {
                // Final text fragment
                richTextFragments.Add(new RichTextFragment(xhtml.Substring(lastFragmentIndex)));
            }

            return new RichText(richTextFragments);
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