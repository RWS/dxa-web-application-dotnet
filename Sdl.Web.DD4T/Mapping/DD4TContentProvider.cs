using System;
using System.Collections.Generic;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.ContentProvider;
using Sdl.Web.Tridion.Query;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Content Provider implementation for DD4T - retrieves DD4T model content
    /// </summary>
    public class DD4TContentProvider : BaseContentProvider
    {
        private readonly ILinkFactory _linkFactory;
        private readonly IPageFactory _pageFactory;

        public DD4TContentProvider(ILinkFactory linkFactory, IModelBuilder modelBuilder, IPageFactory pageFactory, IContentResolver resolver)
        {
            _linkFactory = linkFactory;
            DefaultModelBuilder = modelBuilder;
            _pageFactory = pageFactory;
            ContentResolver = resolver;
        }

        public override string GetPageContent(string url)
        {
            Log.Debug("DD4TContentProvicer.GetPageContent: Processing request for page: {0}", url);
            string page;
            if (_pageFactory != null)
            {
                if (_pageFactory.TryFindPageContent(string.Format("{0}{1}", url.StartsWith("/") ? String.Empty : "/", url), out page))
                {
                    return page;
                }
            }
            else
            {
                throw new ConfigurationException("No PageFactory configured");
            }

            return page;
        }

        //TODO - to get DCP content as object
        public override EntityModel GetEntityModel(string id)
        {
            throw new NotImplementedException("This feature will be implemented in a future release");
        }

        //TODO - to get DCP content as string
        public override string GetEntityContent(string url)
        {
            throw new NotImplementedException("This feature will be implemented in a future release");
        }

        /// <summary>
        /// Execute a broker query to populate a list of teasers
        /// </summary>
        /// <param name="list">The list definition</param>
        public override ContentList<Teaser> PopulateDynamicList(ContentList<Teaser> list)
        {
            var listData = list;
            BrokerQuery query = new BrokerQuery
            {
                Start = list.Start,
                PublicationId = Int32.Parse(WebRequestContext.Localization.LocalizationId),
                PageSize = list.PageSize,
                SchemaId = MapSchema(list.ContentType.Key),
                Sort = list.Sort.Key
            };
            listData.ItemListElements = query.ExecuteQuery();
            foreach (var item in listData.ItemListElements)
            {
                item.Link.Url = ContentResolver.ResolveLink(item.Link.Url);
            }
            listData.HasMore = query.HasMore;
            return listData;
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
        
        protected override object GetPageModelFromUrl(string url)
        {
            if (_pageFactory != null)
            {
                IPage page;
                if (_pageFactory.TryFindPage(string.Format("{0}{1}", url.StartsWith("/") ? String.Empty : "/", url), out page))
                {
                    return page;
                }
            }
            else
            {
                throw new ConfigurationException("No PageFactory configured");
            }

            return null;
        }
        
        protected virtual int MapSchema(string schemaKey)
        {
            var bits = schemaKey.Split('.');
            string moduleName = bits.Length > 1 ? bits[0] : SiteConfiguration.CoreModuleName;
            schemaKey = bits.Length > 1 ? bits[1] : bits[0];
            int res;
            var schemaId = SiteConfiguration.GetConfig(String.Format("{0}.schemas.{1}",moduleName, schemaKey ),WebRequestContext.Localization);
            Int32.TryParse(schemaId, out res);
            return res;
        }

        protected override List<PageModel> GetIncludesFromModel(object model)
        {
            List<PageModel> result = new List<PageModel>();
            if (model is IPage)
            {
                IPage page = (IPage)model;
                string[] bits = page.PageTemplate.Id.Split('-');
                List<string> includes = SemanticMapping.GetIncludes(bits[1], WebRequestContext.Localization);
                if (includes != null)
                {
                    foreach (string include in includes)
                    {
                        PageModel item = GetPageModel(SiteConfiguration.LocalizeUrl(include, WebRequestContext.Localization), addIncludes: false);
                        if (item != null)
                        {
                            result.Add(item);
                        }
                    }
                }
            }
            return result;
        }
    }
}
