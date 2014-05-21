using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using Sdl.Web.DD4T.Mapping;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;

namespace Sdl.Web.DD4T
{
    public class DD4TContentProvider : BaseContentProvider
    {
        public ExtensionlessLinkFactory LinkFactory { get; set; }

        public DD4TContentProvider()
        {
            LinkFactory = new ExtensionlessLinkFactory();
            DefaultModelBuilder = new DD4TViewModelBuilder();
        }

        public override object CreatePageModel(object data, Dictionary<string, object> subPages = null, string view = null)
        {
            if (view == null)
            {
                view = GetPageViewName(data);
            }
            IPage page = data as IPage;
            if (page != null)
            {
                // strip possible numbers from title
                string title = Regex.Replace(page.Title, @"^\d{3}\s", String.Empty);
                // Index is not a proper title for an HTML page
                if (title.ToLowerInvariant().Equals("index"))
                {
                    // TODO get from configuration somewhere
                    title = "Saint John";
                }

                WebPage model = new WebPage { Id = page.Id, Title = title };
                bool first = true;
                foreach (var cp in page.ComponentPresentations)
                {
                    string regionName = GetRegionFromComponentPresentation(cp);
                    if (!model.Regions.ContainsKey(regionName))
                    {
                        model.Regions.Add(regionName, new Region { Name = regionName });
                    }
                    model.Regions[regionName].Items.Add(cp);

                    // determine title and description from first component in main region
                    if (first && regionName.Equals("Main"))
                    {
                        first = false;

                        // TODO use semantic mapping
                        IFieldSet metadata = cp.Component.MetadataFields;
                        IFieldSet fields = cp.Component.Fields;
                        // determine title
                        if (metadata.ContainsKey("standardMeta") && metadata["standardMeta"].EmbeddedValues.Count > 0)
                        {
                            IFieldSet standardMeta = metadata["standardMeta"].EmbeddedValues[0];
                            if (standardMeta.ContainsKey("name"))
                            {
                                model.Title = standardMeta["name"].Value;
                            }

                            // determine description
                            if (standardMeta.ContainsKey("description"))
                            {
                                model.Meta.Add("description", standardMeta["description"].Value);
                            }
                        }
                        else if (fields.ContainsKey("headline"))
                        {
                            // TODO use semantic mapping
                            model.Title = fields["headline"].Value;
                        }
                    }
                }

                //Add header/footer
                if (subPages != null)
                {
                    if (subPages.ContainsKey("Header"))
                    {
                        WebPage headerPage = (WebPage)CreatePageModel(subPages["Header"]);
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
                                        Teaser headerTeaser = CreateEntityModel(region.Value.Items[0], typeof(Teaser)) as Teaser;
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
                        WebPage footerPage = (WebPage)CreatePageModel(subPages["Footer"]);
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
                                        LinkList footerLinks = CreateEntityModel(region.Value.Items[0], typeof(LinkList)) as LinkList;
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

        private static string GetRegionFromComponentPresentation(IComponentPresentation cp)
        {
            var match = Regex.Match(cp.ComponentTemplate.Title, @".*?\[(.*?)\]");
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
