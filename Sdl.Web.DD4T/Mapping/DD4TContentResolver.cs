using System;
using System.Linq;
using System.Web;
using System.Xml;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Tridion;
using Sdl.Web.Common;
using Sdl.Web.Models;
using DD4T.ContentModel;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using interfaces = Sdl.Web.Models.Interfaces;

namespace Sdl.Web.DD4T
{
    public class DD4TContentResolver : IContentResolver
    {
        readonly IComponentFactory ComponentFactory;
        readonly ILinkFactory ComponentLinkProvider;

        public string DefaultExtensionLessPageName { get; set; }
        public string DefaultPageName { get; set; }
        public string DefaultExtension { get; set; }

        public DD4TContentResolver(ILinkFactory componentLinkProvider, IComponentFactory componentFactory)
        {
            ComponentLinkProvider = componentLinkProvider;
            ComponentFactory = componentFactory;
            DefaultExtension = ".html";
            DefaultExtensionLessPageName = Configuration.GetDefaultDocument();
            DefaultPageName = DefaultExtensionLessPageName + DefaultExtension;
        }

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
                if (url.EndsWith(DefaultExtension))
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

        public object ResolveContent(object xml, object resolveInstruction = null)
        {
            if (xml is XmlDocument)
            {
                return ResolveRichText(xml as XmlDocument);
            }
            else
            {
                string content = xml.ToString();
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
        }

        public MvcData ResolveMvcData(object data)
        {
            var res = new MvcData();
            if (data is IComponentPresentation)
            {
                var cp = data as IComponentPresentation;
                var template = cp.ComponentTemplate;
                var viewName = Regex.Replace(template.Title, @"\[.*\]|\s", "");
                    
                if (template.MetadataFields != null)
                {
                    if (template.MetadataFields.ContainsKey("view"))
                    {
                        viewName = template.MetadataFields["view"].Value;
                    }
                }
                res = BuildViewData(viewName);
                //Defaults
                res.ControllerName = Configuration.GetEntityController();
                res.ControllerAreaName = Configuration.GetDefaultModuleName();
                res.ActionName = Configuration.GetEntityAction();
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
                var viewName = page.PageTemplate.Title.Replace(" ", "");
                if (page.PageTemplate.MetadataFields != null)
                {
                    if (page.PageTemplate.MetadataFields.ContainsKey("view"))
                    {
                        viewName = page.PageTemplate.MetadataFields["view"].Value;
                    }
                }
                res = BuildViewData(viewName);
                res.ControllerName = Configuration.GetPageController();
                res.ControllerAreaName = Configuration.GetDefaultModuleName();
                res.ActionName = Configuration.GetPageController();
            }
            else if (data is interfaces.IRegion)
            {
                var region = data as interfaces.IRegion;
                var viewName = region.Name.Replace(" ", "");
                res = BuildViewData(viewName);
                res.ControllerName = Configuration.GetRegionController();
                res.ActionName = Configuration.GetRegionAction();
                res.ControllerAreaName = Configuration.GetDefaultModuleName();
                res.AreaName = region.Module;
            }
            return res;
        }

        protected virtual MvcData BuildViewData(string viewName)
        {
            var bits = viewName.Split(':');
            var areaName = Configuration.GetDefaultModuleName();
            if (bits.Length > 1)
            {
                areaName = bits[0].Trim();
                viewName = bits[1].Trim();
            }
            else
            {
                viewName = bits[0].Trim();
            }
            return new MvcData() { ViewName = viewName, AreaName = areaName };
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