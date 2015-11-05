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
using Sdl.Web.Common.Mapping;
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

        #region IRichTextProcessor Members

        /// <summary>
        /// Processes rich text (XHTML) content.
        /// </summary>
        /// <param name="xhtml">The rich text content (XHTML fragment) to be processed.</param>
        /// <param name="localization">Context localization.</param>
        /// <returns>The processed rich text content.</returns>
        /// <remarks>
        /// Typical rich text processing tasks: 
        /// <list type="bullet">
        ///     <item>Convert XHTML to plain HTML</item>
        ///     <item>Resolve inline links</item>
        /// </list>
        /// </remarks>
        public RichText ProcessRichText(string xhtml, Localization localization)
        {
            try
            {
                XmlDocument xhtmlDoc = new XmlDocument();
                xhtmlDoc.LoadXml(String.Format("<xhtml>{0}</xhtml>", xhtml));
                return ResolveRichText(xhtmlDoc, localization);
            }
            catch (XmlException ex)
            {
                Log.Warn("An error occurred parsing XHTML fragment; rich text processing is skipped: {0}\nXHTML fragment:\n{1}", ex.Message, xhtml);
                return new RichText(xhtml);
            }
        }

        #endregion

        private RichText ResolveRichText(XmlDocument doc, Localization localization)
        {
            const string xlinkNamespaceUri = "http://www.w3.org/1999/xlink";

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("xhtml", "http://www.w3.org/1999/xhtml");
            nsmgr.AddNamespace("xlink", xlinkNamespaceUri);

            // Process/resolve hyperlinks with XLink attributes
            ILinkResolver linkResolver = SiteConfiguration.LinkResolver;
            foreach (XmlElement linkElement in doc.SelectNodes("//a[@xlink:href]", nsmgr))
            {
                // DD4T BinaryPublisher may have resolved a href attribute already (for links to MM Components)
                string linkUrl = linkElement.GetAttribute("href");
                if (string.IsNullOrEmpty(linkUrl))
                {
                    // No href attribute found. Apparently the XLink refers to a regular Component; we resolve it to a URL here.
                    string tcmUri = linkElement.GetAttribute("href", xlinkNamespaceUri);
                    if (!string.IsNullOrEmpty(tcmUri))
                    {
                        // Try to resolve directly to Binary content of MM Component.
                        linkUrl = linkResolver.ResolveLink(tcmUri, resolveToBinary: true);
                    }
                }                
                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // The link was resolved; set HTML href attribute
                    linkElement.SetAttribute("href", linkUrl);
                    ApplyHashIfApplicable(linkElement, localization);

                    // Remove all XLink and data- attributes
                    IEnumerable<XmlAttribute> attributesToRemove = linkElement.Attributes.Cast<XmlAttribute>()
                        .Where(a => a.NamespaceURI == xlinkNamespaceUri || a.LocalName == "xlink" || a.LocalName.StartsWith("data-")).ToArray();
                    foreach (XmlAttribute attr in attributesToRemove)
                    {
                        linkElement.Attributes.Remove(attr);
                    }
                }
                else
                {
                    // The link was not resolved; remove the hyperlink.
                    XmlNode parentNode = linkElement.ParentNode;
                    foreach (XmlNode childNode in linkElement.ChildNodes)
                    {
                        parentNode.InsertBefore(childNode, linkElement);
                    }
                    parentNode.RemoveChild(linkElement);
                }
            }

            // Resolve embedded media items
            List<EntityModel> embeddedEntities = new List<EntityModel>();
            foreach (XmlElement imgElement in doc.SelectNodes("//img[@data-schemaUri]", nsmgr))
            {
                string[] schemaTcmUriParts = imgElement.GetAttribute("data-schemaUri").Split('-');
                SemanticSchema semanticSchema = SemanticMapping.GetSchema(schemaTcmUriParts[1], localization);

                // The semantic mapping may resolve to a more specific model type than specified here (e.g. YouTubeVideo instead of just MediaItem)
                Type modelType = semanticSchema.GetModelTypeFromSemanticMapping(typeof(MediaItem));
                MediaItem mediaItem = (MediaItem)Activator.CreateInstance(modelType);
                mediaItem.ReadFromXhtmlElement(imgElement);
                if (mediaItem.MvcData == null)
                {
                    // In DXA 1.1 MediaItem.ReadFromXhtmlElement was expected to set MvcData.
                    // In DXA 1.2 this should be done in a GetDefaultView override (which is also used for other embedded Entities)
                    mediaItem.MvcData = mediaItem.GetDefaultView(localization);
                }
                embeddedEntities.Add(mediaItem);

                // Replace img element with marker XML processing instruction 
                imgElement.ParentNode.ReplaceChild(
                    doc.CreateProcessingInstruction(EmbeddedEntityProcessingInstructionName, String.Empty),
                    imgElement
                    );
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

        private static void ApplyHashIfApplicable(XmlElement linkElement, Localization localization)
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

            string linkTitle = GetLinkTitle(linkElement, localization);

            string fragmentId = string.Empty;
            if (!string.IsNullOrEmpty(linkTitle))
            {
                fragmentId = '#' + linkTitle.Replace(" ", "_").ToLower();
            }

            linkElement.SetAttribute("href", (!samePage ? href : string.Empty) + fragmentId);
            linkElement.SetAttribute("target", !samePage ? "_top" : string.Empty);
        }

        private static string GetLinkTitle(XmlElement linkElement, Localization localization)
        {
            string componentUri = linkElement.GetAttribute("xlink:href");
            IComponentFactory componentFactory = DD4TFactoryCache.GetComponentFactory(localization);
            IComponent component = componentFactory.GetComponent(componentUri);
            return (component == null) ? linkElement.GetAttribute("title") : component.Title;
        }

    }
}