using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using DD4T.ContentModel;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.DD4T.Utils;
using Sdl.Web.Mvc.Html;
using Sdl.Web.Tridion.Linking;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Content Resolver implementation for DD4T, resolves links and RTF content
    /// </summary>
    public class DD4TContentResolver : IContentResolver
    {
        readonly IComponentFactory _componentFactory;
        readonly ILinkFactory _componentLinkProvider;

        public string DefaultExtensionLessPageName { get; set; }
        public string DefaultPageName { get; set; }
        public string DefaultExtension { get; set; }

        public DD4TContentResolver(ILinkFactory componentLinkProvider, IComponentFactory componentFactory)
        {
            _componentLinkProvider = componentLinkProvider;
            _componentFactory = componentFactory;
            DefaultExtension = ".html";
            DefaultExtensionLessPageName = "index";
            DefaultPageName = DefaultExtensionLessPageName + DefaultExtension;
        }

        /// <summary>
        /// Resolve a link into a URL
        /// </summary>
        /// <param name="linkData">The TCM URI of the item to resolve a link to</param>
        /// <param name="resolveInstruction">The publication ID (int)</param>
        /// <returns></returns>
        public virtual string ResolveLink(object linkData, object resolveInstruction = null)
        {
            var url = linkData as String;
            var localizationId = resolveInstruction as String;
            if (url != null)
            {
                if (url.StartsWith("tcm:"))
                {
                    int pubid = 0;
                    if (localizationId != null)
                    {
                        Int32.TryParse(localizationId, out pubid);
                    }
                    url = TridionHelper.ResolveLink(url, pubid);
                }
                if (url!=null && url.EndsWith(DefaultExtension))
                {
                    url = url.Substring(0, url.Length - DefaultExtension.Length);
                    if (url.EndsWith("/" + DefaultExtensionLessPageName))
                    {
                        url = url.Substring(0, url.Length - DefaultExtensionLessPageName.Length);
                    }
                }
            }
            return url;
        }

        /// <summary>
        /// Resolve RTF content for dynamic links, embedded media content etc.
        /// </summary>
        /// <param name="xml">Xml of RTF content (XmlDocument or xml string)</param>
        /// <param name="resolveInstruction"></param>
        /// <returns></returns>
        public object ResolveContent(object xml, object resolveInstruction = null)
        {
            if (xml is XmlDocument)
            {
                return ResolveRichText(xml as XmlDocument);
            }

            //string content = xml.ToString();
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(String.Format("<xhtml>{0}</xhtml>", xml));
                return ResolveRichText(doc);
            }
            catch (XmlException)
            {
                return xml;
            }
        }

        /// <summary>
        /// Determine MVC data such as view, controller and area name from a Component Presentation, Region or Page
        /// </summary>
        /// <param name="data">The component presentation, region or page object</param>
        /// <returns></returns>
        public MvcData ResolveMvcData(object data)
        {
            var res = new MvcData();
            if (data is IComponentPresentation)
            {
                var cp = data as IComponentPresentation;
                var template = cp.ComponentTemplate;
                var viewName = Regex.Replace(template.Title, @"\[.*\]|\s", String.Empty);
                    
                if (template.MetadataFields != null)
                {
                    if (template.MetadataFields.ContainsKey("view"))
                    {
                        viewName = template.MetadataFields["view"].Value;
                    }
                }
                res = BuildViewData(viewName);
                //Defaults
                res.ControllerName = SiteConfiguration.GetEntityController();
                res.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
                res.ActionName = SiteConfiguration.GetEntityAction();
                res.RouteValues = new Dictionary<string, string>(); 
                
                if (template.MetadataFields !=null)
                {
                    if (template.MetadataFields.ContainsKey("controller"))
                    {
                        var bits = template.MetadataFields["controller"].Value.Split(':');
                        if (bits.Length > 1)
                        {
                            res.ControllerName = bits[1];
                            res.ControllerAreaName = bits[0];
                        }
                        else
                        {
                            res.ControllerName = bits[0];
                        }
                    }
                    if (template.MetadataFields.ContainsKey("regionView"))
                    {
                        var bits = template.MetadataFields["regionView"].Value.Split(':');
                        if (bits.Length > 1)
                        {
                            res.RegionName = bits[1];
                            res.RegionAreaName = bits[0];
                        }
                        else
                        {
                            res.RegionName = bits[0];
                            res.RegionAreaName = SiteConfiguration.GetDefaultModuleName();
                        }
                    }
                    if (template.MetadataFields.ContainsKey("action"))
                    {
                        res.ActionName = template.MetadataFields["action"].Value;
                    }
                    if (template.MetadataFields.ContainsKey("routeValues"))
                    {
                        var bits = template.MetadataFields["routeValues"].Value.Split(',');
                        foreach (string bit in bits)
                        {
                            var parameter = bit.Trim().Split(':');
                            if (parameter.Length > 1 && !res.RouteValues.ContainsKey(parameter[0]))
                            {
                                res.RouteValues.Add(parameter[0],parameter[1]);
                            }
                        }
                    }
                }
            }
            else if (data is IPage)
            {
                var page = data as IPage;
                var viewName = page.PageTemplate.Title.RemoveSpaces();
                if (page.PageTemplate.MetadataFields != null)
                {
                    if (page.PageTemplate.MetadataFields.ContainsKey("view"))
                    {
                        viewName = page.PageTemplate.MetadataFields["view"].Value;
                    }
                }
                res = BuildViewData(viewName);
                res.ControllerName = SiteConfiguration.GetPageController();
                res.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
                res.ActionName = SiteConfiguration.GetPageController();
            }
            else if (data is IRegion)
            {
                var region = data as IRegion;
                var viewName = region.Name.RemoveSpaces();
                res = BuildViewData(viewName);
                res.ControllerName = SiteConfiguration.GetRegionController();
                res.ActionName = SiteConfiguration.GetRegionAction();
                res.ControllerAreaName = SiteConfiguration.GetDefaultModuleName();
                res.AreaName = region.Module;
            }
            return res;
        }

        protected virtual MvcData BuildViewData(string viewName)
        {
            var bits = viewName.Split(':');
            var areaName = SiteConfiguration.GetDefaultModuleName();
            if (bits.Length > 1)
            {
                areaName = bits[0].Trim();
                viewName = bits[1].Trim();
            }
            else
            {
                viewName = bits[0].Trim();
            }
            return new MvcData { ViewName = viewName, AreaName = areaName };
        }
        
        /// <summary>
        /// Resolve rich text. 
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
                    linkUrl = link.Attributes["xlink:href"].IfNotNull(attr => _componentLinkProvider.ResolveLink(attr.Value));                    
                }                
                if (!string.IsNullOrEmpty(linkUrl))
                {
                    // add href
                    var href = doc.CreateAttribute("href");
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
                        var placeholderImgUrl = SiteConfiguration.MediaHelper.GetResponsiveImageUrl(src, 0, "100%");
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
                    var video = doc.ImportNode(temp.DocumentElement, true);

                    // replace youtube element with actual html
                    youtube.ParentNode.ReplaceChild(video, youtube);
                }
            }

            return doc.DocumentElement.InnerXml;
        }

        void ApplyHashIfApplicable(XmlNode link)
        {
            var target = link.Attributes["target"].IfNotNull(attr => attr.Value.ToLower());

            if("anchored" == target) 
            {
                var href = link.Attributes["href"].Value;

                var samePage = string.Equals(href,
                    HttpContext.Current.Request.Url.AbsolutePath,
                    StringComparison.OrdinalIgnoreCase
                );
                
                var hash = GetLinkName(link).IfNotNull(s => '#' + s.Replace(" ", "_").ToLower());
                link.Attributes["href"].Value = (!samePage ? href : string.Empty) + hash;
                link.Attributes["target"].Value = !samePage ? "_top" : string.Empty;
            }
        }

        string GetLinkName(XmlNode link)
        {
            var componentUri = link.Attributes["xlink:href"].IfNotNull(attr => attr.Value);
            
            return _componentFactory.GetComponent(componentUri).IfNotNull(c => c.Title)
                ?? link.Attributes["title"].IfNotNull(attr => attr.Value);
        }
    }   
}