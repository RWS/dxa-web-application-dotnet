using DD4T.ContentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.DD4T.Extensions;
using System.Text.RegularExpressions;
using System.Collections;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using Sdl.Web.Mvc;
using Sdl.Web.DD4T.Mapping;

namespace Sdl.Web.DD4T
{
    /// <summary>
    /// Default ModelFactory, retrieves model type and sets properties
    /// according to some generic rules
    /// </summary>
    public class DD4TModelFactory : BaseModelFactory
    {
        public ExtensionlessLinkFactory LinkFactory { get; set; }
        public DD4TModelFactory()
        {
            this.LinkFactory = new ExtensionlessLinkFactory();
            DefaultEntityBuilder = new DD4TEntityBuilder();
        }

        public override object CreatePageModel(object data, Dictionary<string,object> subPages = null, string view = null)
        {
            if (view == null)
            {
                view = GetPageViewName(data);
            }
            IPage page = data as IPage;
            if (page != null)
            {
                WebPage model = new WebPage{Id=page.Id,Title=page.Title};
                foreach (var cp in page.ComponentPresentations)
                {
                    string regionName = GetRegionFromComponentPresentation(cp);
                    if (!model.Regions.ContainsKey(regionName))
                    {
                        model.Regions.Add(regionName, new Region { Name = regionName });
                    }
                    model.Regions[regionName].Items.Add(cp);
                }
                //Add header/footer
                if (subPages != null)
                {
                    if (subPages.ContainsKey("Header"))
                    {
                        WebPage headerPage = (WebPage)this.CreatePageModel(subPages["Header"]);
                        if (headerPage != null)
                        {
                            var header = new Header { Regions = new Dictionary<string, Region>() };
                            foreach (var region in headerPage.Regions)
                            {
                                //The main region should contain a Teaser containing the header logo etc.
                                if (region.Key == "Main")
                                {
                                    if (region.Value.Items.Count > 0)
                                    {
                                        Teaser headerTeaser = this.CreateEntityModel(region.Value.Items[0]) as Teaser;
                                        if (headerTeaser != null)
                                        {
                                            header.Logo = headerTeaser.Image;
                                            header.LogoLink = headerTeaser.Link;
                                            header.Heading = headerTeaser.Headline;
                                            header.Subheading = headerTeaser.Text;
                                        }
                                        else
                                        {
                                            Log.Warn("Header 'page' does not contain a Teaser in the Main region. Cannot set logo/heading/subheading");
                                        }
                                    }
                                }
                                //Other regions are simply added to the header regions container
                                else
                                {
                                    header.Regions.Add(region.Key, region.Value);
                                }
                            }
                            model.Header = header;
                        }
                    }
                    if (subPages.ContainsKey("Footer"))
                    {
                        WebPage footerPage = (WebPage)this.CreatePageModel(subPages["Footer"]);
                        if (footerPage != null)
                        {
                            var footer = new Footer { Regions = new Dictionary<string, Region>() };
                            foreach (var region in footerPage.Regions)
                            {
                                //The main region should contain a LinkList containing the footer copyright and links.
                                if (region.Key == "Main")
                                {
                                    if (region.Value.Items.Count > 0)
                                    {
                                        LinkList footerLinks = this.CreateEntityModel(region.Value.Items[0]) as LinkList;
                                        if (footerLinks != null)
                                        {
                                            footer.Copyright = footerLinks.Headline;
                                            footer.Links = footerLinks.Links;
                                        }
                                        else
                                        {
                                            Log.Warn("Footer 'page' does not contain a Teaser in the Main region. Cannot set logo/copyright");
                                        }
                                    }
                                }
                                //Other regions are simply added to the header regions container
                                else
                                {
                                    footer.Regions.Add(region.Key, region.Value);
                                }
                            }
                            model.Footer = footer;
                        }
                    }
                }


                return model;
            }
            throw new Exception(String.Format("Cannot create model for class {0}. Expecting IPage.", data.GetType().FullName));
        }

        private string GetRegionFromComponentPresentation(IComponentPresentation cp)
        {
            var match = Regex.Match(cp.ComponentTemplate.Title,@".*?\[(.*?)\]");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            //default region name
            return "Main";
        }

        public override string GetPageViewName(object pageObject)
        {
            var page = (IPage)pageObject;
            var viewName = page.PageTemplate.Title.Replace(" ", "");
            var module = Configuration.GetDefaultModuleName();
            if (page.PageTemplate.MetadataFields != null)
            {
                if (page.PageTemplate.MetadataFields.ContainsKey("view"))
                {
                    viewName = page.PageTemplate.MetadataFields["view"].Value;
                }
                if (page.PageTemplate.MetadataFields.ContainsKey("module"))
                {
                    module = page.PageTemplate.MetadataFields["module"].Value;
                }
            }
            return String.Format("{0}/{1}", module, viewName);
        }

        public override string GetEntityViewName(object entity)
        {
            var componentPresentation = (ComponentPresentation)entity;
            var template = componentPresentation.ComponentTemplate;
            //strip region and whitespace
            string viewName = Regex.Replace(template.Title, @"\[.*\]|\s", "");
            var module = Configuration.GetDefaultModuleName();
            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("view"))
                {
                    viewName = componentPresentation.ComponentTemplate.MetadataFields["view"].Value;
                }
                if (template.MetadataFields.ContainsKey("module"))
                {
                    module = componentPresentation.ComponentTemplate.MetadataFields["module"].Value;
                }
            }
            return String.Format("{0}/{1}", module, viewName);
        }
     }
}
