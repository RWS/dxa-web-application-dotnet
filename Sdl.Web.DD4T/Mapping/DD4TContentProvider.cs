using System;
using System.Collections.Generic;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Models;
using Sdl.Web.Mvc;
using Sdl.Web.Tridion;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    public class DD4TContentProvider : BaseContentProvider
    {
        readonly ILinkFactory _linkFactory;
        readonly IPageFactory _pageFactory;

        public DD4TContentProvider(ILinkFactory linkFactory, IModelBuilder modelBuilder, IPageFactory pageFactory, IContentResolver resolver)
        {
            ContentResolver = resolver;
            _linkFactory = linkFactory;
            DefaultModelBuilder = modelBuilder;
            _pageFactory = pageFactory;
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
            return new MvcData { ViewName = viewName, AreaName = areaName };
        }
        
        protected override object GetPageModelFromUrl(string url)
        {
            if (_pageFactory != null)
            {
                DateTime timerStart = DateTime.Now;
                IPage page;
                if (_pageFactory.TryFindPage(string.Format("{0}{1}", url.StartsWith("/") ? "" : "/", url), out page))
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
            if (_pageFactory != null)
            {
                DateTime timerStart = DateTime.Now;
                if (_pageFactory.TryFindPageContent(string.Format("{0}{1}", url.StartsWith("/") ? "" : "/", url), out page))
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
            BrokerQuery query = new BrokerQuery
                {
                    Start = list.Start,
                    PublicationId = Int32.Parse(WebRequestContext.Localization.LocalizationId),
                    PageSize = list.PageSize,
                    SchemaId = MapSchema(list.ContentType.Key),
                    Sort = list.Sort.Key
                };
            list.ItemListElements = query.ExecuteQuery();
            foreach (var item in list.ItemListElements)
            {
                item.Link.Url = ContentResolver.ResolveLink(item.Link.Url);
            }
            list.HasMore = query.HasMore;
            Log.Trace(timerStart, "list-load", list.Headline ?? "List");
        }

        protected virtual int MapSchema(string schemaKey)
        {
            var bits = schemaKey.Split('.');
            string moduleName = bits.Length > 1 ? bits[0] : Configuration.CoreModuleName;
            schemaKey = bits.Length > 1 ? bits[1] : bits[0];
            int res;
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
                        var item = GetPageModel(Configuration.LocalizeUrl(include, WebRequestContext.Localization));
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
