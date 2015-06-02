using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Models;
using Sdl.Web.DD4T.Statics;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Tridion.Query;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Default (DD4T-based) Content Provider and Navigation Provider implementation.
    /// </summary>
    public class DD4TProvider : IContentProvider, INavigationProvider
    {
        private readonly IPageFactory _pageFactory;

        /// <summary>
        /// Model Builder used to map DD4T types to DXA View Models.
        /// </summary>
        protected IModelBuilder ModelBuilder 
        { 
            get; 
            private set; 
        }

        protected IPageFactory PageFactory
        {
            get
            {
                _pageFactory.PageProvider.PublicationId = 0; // Force the DD4T PageProvider to use our PublicationResolver to determine the Publication ID.
                return _pageFactory;
            }
        }

        public DD4TProvider(IModelBuilder modelBuilder, IPageFactory pageFactory)
        {
            if (modelBuilder == null)
            {
                throw new DxaException("No Model Builder configured.");
            }
            if (pageFactory == null)
            {
                throw new DxaException("No Page Factory configured.");
            }

            ModelBuilder = modelBuilder;
            _pageFactory = pageFactory;
        }

        #region IContentProvider members
#pragma warning disable 618
        public IContentResolver ContentResolver
        {
            get
            {
                return new LegacyContentResolverFacade();
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1.");
            }
        }
#pragma warning restore 618

        /// <summary>
        /// Get the model for a page given the URL
        /// </summary>
        /// <param name="url">Page URL</param>
        /// <param name="addIncludes">If true then includes will be added in the model</param>
        /// <returns>Model corresponding to that URL</returns>
        public virtual PageModel GetPageModel(string url, bool addIncludes)
        {
            using (new Tracer(url, addIncludes))
            {
                string cmsUrl = GetCmUrl(url);
                Log.Debug("Trying CM URL '{0}'", cmsUrl);

                //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
                //1. Try adding the default extension, so /news becomes /news.html
                IPage page = GetPageModelFromUrl(cmsUrl);
                if (page == null && (url == null || (!url.EndsWith("/") && url.LastIndexOf(".", StringComparison.Ordinal) <= url.LastIndexOf("/", StringComparison.Ordinal))))
                {
                    //2. Try adding the default page, so /news becomes /news/index.html
                    cmsUrl = GetCmUrl(url + "/");
                    Log.Debug("No content found, trying default Page CM URL: '{0}'", cmsUrl);
                    page = GetPageModelFromUrl(cmsUrl);
                }
                if (page == null)
                {
                    return null;
                }

                MvcData viewData = DD4TMappingUtilities.ResolveMvcData(page);
                Type viewModeltype = ModelTypeRegistry.GetViewModelType(viewData);
                List<PageModel> includes = addIncludes ? GetIncludesFromModel(page) : new List<PageModel>();
                return ModelBuilder.CreatePageModel(page, viewModeltype, includes, viewData);
            }
        }

        //TODO TSI-803 - to get DCP content as object
        public virtual EntityModel GetEntityModel(string id)
        {
            using (new Tracer(id))
            {
                throw new NotImplementedException("This feature will be implemented in a future release");
            }
        }



        /// <summary>
        /// Gets a Static Content Item for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Static Content Item.</returns>
        public StaticContentItem GetStaticContentItem(string urlPath, Localization localization)
        {
            using (new Tracer(urlPath, localization))
            {
                string localFilePath = BinaryFileManager.Instance.GetCachedFile(urlPath, localization);
 
                return new StaticContentItem(
                    new FileStream(localFilePath, FileMode.Open),
                    MimeMapping.GetMimeMapping(localFilePath),
                    File.GetLastWriteTime(localFilePath), 
                    Encoding.UTF8
                    );
            }
        }

        /// <summary>
        /// Populates a Content List (of Teasers) by executing the query it specifies.
        /// </summary>
        /// <param name="contentList">The Content List which specifies the query and is to be populated.</param>
        public virtual void PopulateDynamicList<T>(ContentList<T> contentList) where T:EntityModel
        {
            using (new Tracer(contentList))
            {
                BrokerQuery query = new BrokerQuery
                {
                    Start = contentList.Start,
                    PublicationId = Int32.Parse(WebRequestContext.Localization.LocalizationId),
                    PageSize = contentList.PageSize,
                    SchemaId = MapSchema(contentList.ContentType.Key),
                    Sort = contentList.Sort.Key
                };

                // TODO: For now BrokerQuery always returns Teasers
                IEnumerable<Teaser> queryResults = query.ExecuteQuery();

                ILinkResolver linkResolver = SiteConfiguration.LinkResolver;
                foreach (Teaser item in queryResults)
                {
                    item.Link.Url = linkResolver.ResolveLink(item.Link.Url);
                }

                contentList.ItemListElements = queryResults.Cast<T>().ToList();
                contentList.HasMore = query.HasMore;
            }
        }

        #endregion

        #region INavigationProvider Members

        /// <summary>
        /// Gets the Navigation Model (Sitemap) for a given Localization.
        /// </summary>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Model (Sitemap root Item).</returns>
        public virtual SitemapItem GetNavigationModel(Localization localization)
        {
            using (new Tracer(localization))
            {
                string url = SiteConfiguration.LocalizeUrl("navigation.json", localization);
                // TODO TSI-110: This is a temporary measure to cache the Navigation Model per request to not retrieve and serialize 3 times per request. Comprehensive caching strategy pending
                string cacheKey = "navigation-" + url;
                SitemapItem result;
                if (HttpContext.Current.Items[cacheKey] == null)
                {
                    Log.Debug("Deserializing Navigation Model from raw content URL '{0}'", url);
                    string navigationJsonString = GetPageContent(url);
                    result = new JavaScriptSerializer().Deserialize<SitemapItem>(navigationJsonString);
                    HttpContext.Current.Items[cacheKey] = result;
                }
                else
                {
                    Log.Debug("Obtained Navigation Model from cache.");
                    result = (SitemapItem)HttpContext.Current.Items[cacheKey];
                }
                return result;
            }
        }

        /// <summary>
        /// Gets Navigation Links for the top navigation menu for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public virtual NavigationLinks GetTopNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                NavigationLinks navigationLinks = new NavigationLinks();
                SitemapItem sitemapRoot = GetNavigationModel(localization);
                foreach (SitemapItem item in sitemapRoot.Items.Where(i => i.Visible))
                {
                    navigationLinks.Items.Add(CreateLink((item.Title == "Index") ? sitemapRoot : item));
                }
                return navigationLinks;
            }
        }

        /// <summary>
        /// Gets Navigation Links for the context navigation panel for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public virtual NavigationLinks GetContextNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {
                NavigationLinks navigationLinks = new NavigationLinks();
                SitemapItem sitemapItem = GetNavigationModel(localization); // Start with Sitemap root Item.
                int levels = requestUrlPath.Split('/').Length;
                while (levels > 1 && sitemapItem.Items != null)
                {
                    SitemapItem newParent = sitemapItem.Items.FirstOrDefault(i => i.Type == "StructureGroup" && requestUrlPath.StartsWith(i.Url.ToLower()));
                    if (newParent == null)
                    {
                        break;
                    }
                    sitemapItem = newParent;
                }

                if (sitemapItem != null && sitemapItem.Items != null)
                {
                    foreach (SitemapItem item in sitemapItem.Items.Where(i => i.Visible))
                    {
                        navigationLinks.Items.Add(CreateLink(item));
                    }
                }

                return navigationLinks;
            }
        }

        /// <summary>
        /// Gets Navigation Links for the breadcrumb trail for the given request URL path.
        /// </summary>
        /// <param name="requestUrlPath">The request URL path.</param>
        /// <param name="localization">The Localization.</param>
        /// <returns>The Navigation Links.</returns>
        public virtual NavigationLinks GetBreadcrumbNavigationLinks(string requestUrlPath, Localization localization)
        {
            using (new Tracer(requestUrlPath, localization))
            {

                NavigationLinks navigationLinks = new NavigationLinks();
                int levels = requestUrlPath.Split('/').Length;
                SitemapItem sitemapItem = GetNavigationModel(localization); // Start with Sitemap root Item.
                navigationLinks.Items.Add(CreateLink(sitemapItem));
                while (levels > 1 && sitemapItem.Items != null)
                {
                    sitemapItem = sitemapItem.Items.FirstOrDefault(i => requestUrlPath.StartsWith(i.Url.ToLower()));
                    if (sitemapItem != null)
                    {
                        navigationLinks.Items.Add(CreateLink(sitemapItem));
                        levels--;
                    }
                    else
                    {
                        break;
                    }
                }
                return navigationLinks;
            }
        }

        #endregion

        /// <summary>
        /// Creates a Link Entity Model out of a SitemapItem Entity Model.
        /// </summary>
        /// <param name="sitemapItem">The SitemapItem Entity Model.</param>
        /// <returns>The Link Entity Model.</returns>
        protected static Link CreateLink(SitemapItem sitemapItem)
        {
            return new Link
            {
                Url = SiteConfiguration.LinkResolver.ResolveLink(sitemapItem.Url),
                LinkText = sitemapItem.Title
            };
        }

        /// <summary>
        /// Converts a request URL into a CMS URL (for example adding default page name, and file extension)
        /// </summary>
        /// <param name="url">The request URL</param>
        /// <returns>A CMS URL</returns>
        protected virtual string GetCmUrl(string url)
        {
            if (String.IsNullOrEmpty(url))
            {
                url = Constants.DefaultPageName;
            }
            if (url.EndsWith("/"))
            {
                url = url + Constants.DefaultPageName;
            }
            if (!Path.HasExtension(url))
            {
                url = url + Constants.DefaultExtension;
            }
            return url;
        }

        private static string NormalizeUrl(string url)
        {
            return url.StartsWith("/") ? url : ("/" + url);
        }

        protected virtual string GetPageContent(string url)
        {
            string page;
            PageFactory.TryFindPageContent(NormalizeUrl(url), out page);
            return page;
        }

        protected virtual IPage GetPageModelFromUrl(string url)
        {
            IPage page;
            PageFactory.TryFindPage(NormalizeUrl(url), out page);
            return page;
        }
        
        protected virtual int MapSchema(string schemaKey)
        {
            string[] schemaKeyParts = schemaKey.Split('.');
            string moduleName = schemaKeyParts.Length > 1 ? schemaKeyParts[0] : SiteConfiguration.CoreModuleName;
            schemaKey = schemaKeyParts.Length > 1 ? schemaKeyParts[1] : schemaKeyParts[0];
            string schemaId = SiteConfiguration.GetConfig(String.Format("{0}.schemas.{1}",moduleName, schemaKey ),WebRequestContext.Localization);

            int result;
            Int32.TryParse(schemaId, out result);
            return result;
        }

        protected virtual List<PageModel> GetIncludesFromModel(IPage page)
        {
            List<PageModel> result = new List<PageModel>();
            string[] bits = page.PageTemplate.Id.Split('-');
            List<string> includes = SemanticMapping.GetIncludes(bits[1], WebRequestContext.Localization);
            if (includes != null)
            {
                foreach (string include in includes)
                {
                    PageModel item = GetPageModel(SiteConfiguration.LocalizeUrl(include, WebRequestContext.Localization), addIncludes: false);
                    if (item != null)
                    {
                        item.IsIncluded = true;
                        result.Add(item);
                    }
                }
            }
            return result;
        }
    }
}
