using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using Sdl.Web.DD4T.Mapping;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using DD4T.ContentModel.Exceptions;
//TODO  - abstract this dependency
using DD4T.Providers.SDLTridion2013;

namespace Sdl.Web.DD4T
{
    public class DD4TContentProvider : BaseContentProvider
    {
        public ExtensionlessLinkFactory LinkFactory { get; set; }
        public IPageFactory PageFactory { get; set; }

        public DD4TContentProvider()
        {
            LinkFactory = new ExtensionlessLinkFactory();
            DefaultModelBuilder = new DD4TModelBuilder();
            this.PageFactory = new PageFactory()
            {
                PageProvider = new TridionPageProvider(),
                PublicationResolver = new PublicationResolver(),
                ComponentFactory = new ComponentFactory() { PublicationResolver = new PublicationResolver() },
                LinkFactory = new ExtensionlessLinkFactory() { PublicationResolver = new PublicationResolver() }
            };
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

        public override string GetRegionViewName(object region)
        {
            var model = (Region)region;
            var viewName = model.Name.Replace(" ", "");
            var module = Configuration.GetDefaultModuleName();
            return String.Format("{0}/{1}", module, viewName); 
        }

        protected override object GetPageModelFromUrl(string url)
        {
            if (PageFactory != null)
            {
                IPage page = null;
                if (PageFactory.TryFindPage(string.Format("{0}{1}", url.StartsWith("/") ? "" : "/", url), out page))
                {
                    // only set InlineEditingBootstrap when it is not set before (else header and footer pages might override it)
                    /*if (ViewBag.InlineEditingBootstrap == null)
                    {
                        ViewBag.InlineEditingBootstrap = Markup.GetInlineEditingBootstrap(page);
                    }*/
                    return page;
                }
            }
            else
                throw new ConfigurationException("No PageFactory configured");

            return null;
        }

        public override string GetPageContent(string url)
        {
            string page;
            if (PageFactory != null)
            {
                if (PageFactory.TryFindPageContent(string.Format("{0}{1}", url.StartsWith("/") ? "" : "/", url), out page))
                {
                    return page;
                }
            }
            else
                throw new ConfigurationException("No PageFactory configured");

            return page;
        }


        //TODO - to get DCP content as object
        public override object GetEntityModel(string id)
        {
            throw new NotImplementedException();
        }

        //TODO - to get DCP content as string
        public override string GetEntityContent(string url)
        {
            throw new NotImplementedException();
        }

    }
}
