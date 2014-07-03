using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DD4T.ContentModel;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Factories;
using DD4T.Factories;
using Sdl.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Models;
using Sdl.Web.Tridion;
using interfaces = Sdl.Web.Models.Interfaces;

namespace Sdl.Web.DD4T
{
    public class DD4TContentProvider : BaseContentProvider
    {
        readonly ExtensionlessLinkFactory LinkFactory;
        readonly IPageFactory PageFactory;

        public DD4TContentProvider(ExtensionlessLinkFactory linkFactory, IModelBuilder modelBuilder, IPageFactory pageFactory)
        {
            LinkFactory = linkFactory;
            DefaultModelBuilder = modelBuilder;
            this.PageFactory = pageFactory;
        }

        public override string ProcessUrl(string url, string localizationId = null)
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
            return base.ProcessUrl(url);
        }

        public override ViewData GetPageViewData(object pageObject)
        {
            var page = (IPage)pageObject;
            var viewName = page.PageTemplate.Title.Replace(" ", "");
            if (page.PageTemplate.MetadataFields != null)
            {
                if (page.PageTemplate.MetadataFields.ContainsKey("view"))
                {
                    viewName = page.PageTemplate.MetadataFields["view"].Value;
                }
            }
            return BuildViewData(viewName); 
        }

        public override ViewData GetEntityViewData(object entity)
        {
            var componentPresentation = (ComponentPresentation)entity;
            var template = componentPresentation.ComponentTemplate;
            //strip region and whitespace
            string viewName = Regex.Replace(template.Title, @"\[.*\]|\s", "");
            if (template.MetadataFields != null)
            {
                if (template.MetadataFields.ContainsKey("view"))
                {
                    viewName = componentPresentation.ComponentTemplate.MetadataFields["view"].Value;
                }
            }
            return BuildViewData(viewName); 
        }

        public override ViewData GetRegionViewData(object region)
        {
            var model = (interfaces.IRegion)region;
            var viewName = model.Name.Replace(" ", "");
            return BuildViewData(viewName); 
        }

        protected virtual ViewData BuildViewData(string viewName)
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
            return new ViewData() { ViewName = viewName, AreaName = areaName };
        }
        
        protected override object GetPageModelFromUrl(string url)
        {
            if (PageFactory != null)
            {
                DateTime timerStart = DateTime.Now;
                IPage page = null;
                if (PageFactory.TryFindPage(string.Format("{0}{1}", url.StartsWith("/") ? "" : "/", url), out page))
                {
                    Log.Trace(timerStart, "page-load", url);
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
                DateTime timerStart = DateTime.Now;
                if (PageFactory.TryFindPageContent(string.Format("{0}{1}", url.StartsWith("/") ? "" : "/", url), out page))
                {
                    Log.Trace(timerStart, "page-load", url);
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


        public override void PopulateDynamicList(ContentList<Teaser> list)
        {
            DateTime timerStart = DateTime.Now;
            BrokerQuery query = new BrokerQuery();
            query.Start = list.Start;
            query.PublicationId = Int32.Parse(WebRequestContext.Localization.LocalizationId);
            query.PageSize = list.PageSize;
            query.SchemaId = MapSchema(list.ContentType.Key);
            query.Sort = list.Sort.Key;
            list.ItemListElements = query.ExecuteQuery();
            foreach (var item in list.ItemListElements)
            {
                item.Link.Url = this.ProcessUrl(item.Link.Url);
            }
            list.HasMore = query.HasMore;
            Log.Trace(timerStart, "list-load", list.Headline ?? "List");
        }

        protected virtual int MapSchema(string schemaKey)
        {
            var bits = schemaKey.Split('.');
            string moduleName = bits.Length > 1 ? bits[0] : Configuration.CoreModuleName;
            schemaKey = bits.Length > 1 ? bits[1] : bits[0];
            int res = 0;
            var schemaId = Configuration.GetGlobalConfig("schemas." + schemaKey, moduleName);
            Int32.TryParse(schemaId, out res);
            return res;
        }

        protected override List<object> GetIncludesFromModel(object model, ModelType modelType)
        {
            List<object> res = new List<object>();
            if (modelType == ModelType.Page)
            {
                var page = (IPage)model;
                var bits = page.PageTemplate.Id.Split('-');
                var includes = SemanticMapping.GetIncludes(bits[1]);
                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        var item = GetPageModel(Configuration.LocalizeUrl(include));
                        if (item != null)
                        {
                            res.Add(item);
                        }
                    }
                }
            }
            return res;
        }
    }
}
