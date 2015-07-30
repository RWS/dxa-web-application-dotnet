using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using DD4T.ContentModel;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Default Rich Text Processor implementation (DD4T-based).
    /// </summary>
    public class DefaultRichTextProcessor : IRichTextProcessor
    {
        private const string EmbeddedEntityProcessingInstructionName = "EmbeddedEntity";
        private static readonly Regex EmbeddedEntityProcessingInstructionRegex = new Regex(@"<\?EmbeddedEntity\s\?>", RegexOptions.Compiled);
        private readonly IComponentFactory _componentFactory;

        public DefaultRichTextProcessor(IComponentFactory componentFactory)
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
            foreach (XmlElement linkElement in doc.SelectNodes("//a[@xlink:href[starts-with(string(.),'tcm:')]][@href='' or not(@href)]", nsmgr))
            {
                // does this link already have a resolved href?
                string linkUrl = linkElement.GetAttribute("href");
                if (string.IsNullOrEmpty(linkUrl))
                {
                    // DD4T BinaryPublisher resolves these links and adds a src rather than a href, let's try that
                    linkUrl = linkElement.GetAttribute("src");
                    // lets remove that invalid attribute directly 
                    linkElement.RemoveAttribute("src");
                }
                if (string.IsNullOrEmpty(linkUrl))
                {
                    // assume dynamic component link and try to resolve
                    string tcmUri = linkElement.GetAttribute("xlink:href");
                    if (!string.IsNullOrEmpty(tcmUri))
                    {
                        linkUrl = linkResolver.ResolveLink(tcmUri);
                    }
                }                
                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // add href
                    linkElement.SetAttribute("href", linkUrl);

                    ApplyHashIfApplicable(linkElement);

                    // remove all xlink attributes
                    foreach (XmlAttribute xlinkAttr in linkElement.SelectNodes("//@xlink:*", nsmgr))
                    {
                        linkElement.Attributes.Remove(xlinkAttr);
                    }
                }
                else
                {
                    // copy child nodes of link so we keep them
                    linkElement.ChildNodes.Cast<XmlNode>()
                        .Select(linkElement.RemoveChild)
                        .ToList()
                        .ForEach(child => 
                        {
                            linkElement.ParentNode.InsertBefore(child, linkElement);
                        });
                    // remove link node
                    linkElement.ParentNode.RemoveChild(linkElement);
                }
            }

            // Resolve embedded YouTube videos
            List<EntityModel> embeddedEntities = new List<EntityModel>();
            foreach (XmlElement youTubeImgElement in doc.SelectNodes("//img[@data-youTubeId][@xlink:href]", nsmgr))
            {
                YouTubeVideo youTubeVideo = new YouTubeVideo
                {
                    Id =  DefaultModelBuilder.GetDxaIdentifierFromTcmUri(youTubeImgElement.GetAttribute("xlink:href")), 
                    Url = youTubeImgElement.GetAttribute("src"),
                    YouTubeId = youTubeImgElement.GetAttribute("data-youTubeId"),
                    Headline = youTubeImgElement.GetAttribute("data-headline"),
                    IsEmbedded = true,
                    MvcData = new MvcData("Core:Entity:YouTubeVideo")
                };
                embeddedEntities.Add(youTubeVideo);

                // Replace YouTube img element with marker XML processing instruction 
                youTubeImgElement.ParentNode.ReplaceChild(
                    doc.CreateProcessingInstruction(EmbeddedEntityProcessingInstructionName, string.Empty), 
                    youTubeImgElement
                    );
            }

            // resolve embedded ecl items
            embeddedEntities = new List<EntityModel>();
            foreach (XmlElement eclImgElement in doc.SelectNodes("//img[@data-eclUri][@xlink:href]", nsmgr))
            {
                string semanticSchema = eclImgElement.GetAttribute("data-semanticSchema");
                // TODO: use model mapping to find correct model and add it as is done above for youtube video
            }

            // Split the XHTML into fragments based on marker XML processing instructions.
            string xhtml = doc.DocumentElement.InnerXml;
            IList<IRichTextFragment> richTextFragments = new List<IRichTextFragment>();
            int lastFragmentIndex = 0;
            int i = 0;
            foreach (Match embeddedEntityMatch in EmbeddedEntityProcessingInstructionRegex.Matches(xhtml))
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

        private void ApplyHashIfApplicable(XmlElement linkElement)
        {
            string target = linkElement.GetAttribute("target").ToLower();
            if (target != "anchored")
            {
                return;
            }

            string href = linkElement.GetAttribute("href");

            bool samePage = string.Equals(href,
                    HttpContext.Current.Request.Url.AbsolutePath, // TODO: should not be using HttpContext at this level
                    StringComparison.OrdinalIgnoreCase
                    );

            string linkTitle = GetLinkTitle(linkElement);

            string fragmentId = string.Empty;
            if (!string.IsNullOrEmpty(linkTitle))
            {
                fragmentId = '#' + linkTitle.Replace(" ", "_").ToLower();
            }

            linkElement.SetAttribute("href", (!samePage ? href : string.Empty) + fragmentId);
            linkElement.SetAttribute("target", !samePage ? "_top" : string.Empty);
        }

        private string GetLinkTitle(XmlElement linkElement)
        {
            string componentUri = linkElement.GetAttribute("xlink:href");
            IComponent component = _componentFactory.GetComponent(componentUri);
            return (component == null) ? linkElement.GetAttribute("title") : component.Title;
        }

    }
}