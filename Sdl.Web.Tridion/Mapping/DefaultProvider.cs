using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using DD4T.ContentModel;
using DD4T.ContentModel.Factories;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.Statics;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Tridion.Query;
using Tridion.ContentDelivery.DynamicContent.Query;
using Tridion.ContentDelivery.Meta;
using IItem = Tridion.ContentDelivery.Meta.IItem;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Default Content Provider and Navigation Provider implementation (DD4T-based).
    /// </summary>
    public class DefaultProvider : IContentProvider, INavigationProvider
    {

        #region IContentProvider members
#pragma warning disable 618
        [Obsolete("Deprecated in DXA 1.1. Use SiteConfiguration.LinkResolver or SiteConfiguration.RichTextProcessor to get the new extension points.")]
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

        /// <summary>
        /// Gets a Page Model for a given URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        [Obsolete("Deprecated in DXA 1.1. Use the overload that has a Localization parameter.")]
        public PageModel GetPageModel(string url, bool addIncludes = true)
        {
            return GetPageModel(url, WebRequestContext.Localization, addIncludes);
        }

        /// <summary>
        /// Populates a Content List by executing the query it specifies.
        /// </summary>
        /// <param name="contentList">The Content List (of Teasers) which specifies the query and is to be populated.</param>
        [Obsolete("Deprecated in DXA 1.1. Use the overload that has a Localization parameter.")]
        public ContentList<Teaser> PopulateDynamicList(ContentList<Teaser> contentList)
        {
            PopulateDynamicList(contentList, WebRequestContext.Localization);
            return contentList;
        }

#pragma warning restore 618


        /// <summary>
        /// Gets a Page Model for a given URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given URL.</exception>
        public virtual PageModel GetPageModel(string url, Localization localization, bool addIncludes)
        {
            using (new Tracer(url, localization, addIncludes))
            {
                //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
                //1. Try adding the default extension, so /news becomes /news.html
                IPage page = GetPage(url, localization);
                if (page == null && (url == null || (!url.EndsWith("/") && url.LastIndexOf(".", StringComparison.Ordinal) <= url.LastIndexOf("/", StringComparison.Ordinal))))
                {
                    //2. Try adding the default page, so /news becomes /news/index.html
                    page = GetPage(url + "/", localization);
                }
                if (page == null)
                {
                    throw new DxaItemNotFoundException(url, localization.LocalizationId);
                }
                FullyLoadDynamicComponentPresentations(page, localization);

                IPage[] includes = addIncludes ? GetIncludesFromModel(page, localization).ToArray() : new IPage[0];

                return ModelBuilderPipeline.CreatePageModel(page, includes, localization);
            }
        }

        /// <summary>
        /// Gets an Entity Model for a given Entity Identifier.
        /// </summary>
        /// <param name="id">The Entity Identifier in format ComponentID-TemplateID.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The Entity Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Entity Model exists for the given URL.</exception>
        /// <remarks>
        /// Since we can't obtain CT metadata for DCPs, we obtain the View Name from the CT Title.
        /// </remarks>
        public virtual EntityModel GetEntityModel(string id, Localization localization)
        {
            using (new Tracer(id, localization))
            {
                string[] idParts = id.Split('-');
                if (idParts.Length != 2)
                {
                    throw new DxaException(String.Format("Invalid Entity Identifier '{0}'. Must be in format ComponentID-TemplateID.", id));
                }

                string componentUri = string.Format("tcm:{0}-{1}", localization.LocalizationId, idParts[0]);
                string templateUri = string.Format("tcm:{0}-{1}-32", localization.LocalizationId, idParts[1]);

                IComponentPresentationFactory componentPresentationFactory = DD4TFactoryCache.GetComponentPresentationFactory(localization);
                IComponentPresentation dcp;
                if (!componentPresentationFactory.TryGetComponentPresentation(out dcp, componentUri, templateUri))
                {
                    throw new DxaItemNotFoundException(id, localization.LocalizationId);
                }

                return ModelBuilderPipeline.CreateEntityModel(dcp, localization);
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
                    new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan),
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
        /// <param name="localization">The context Localization.</param>
        public virtual void PopulateDynamicList<T>(ContentList<T> contentList, Localization localization) where T : EntityModel
        {
            using (new Tracer(contentList, localization))
            {
                BrokerQuery query = new BrokerQuery
                {
                    Start = contentList.Start,
                    PublicationId = Int32.Parse(localization.LocalizationId),
                    PageSize = contentList.PageSize,
                    SchemaId = MapSchema(contentList.ContentType.Key, localization),
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
                    string navigationJsonString = GetPageContent(url, localization);
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
                    SitemapItem newParent = sitemapItem.Items.FirstOrDefault(i => i.Type == "StructureGroup" && requestUrlPath.StartsWith(i.Url, StringComparison.InvariantCultureIgnoreCase));
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
                    sitemapItem = sitemapItem.Items.FirstOrDefault(i => requestUrlPath.StartsWith(i.Url, StringComparison.InvariantCultureIgnoreCase));
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
            string url = sitemapItem.Url;
            if (url.StartsWith("tcm:"))
            {
                url = SiteConfiguration.LinkResolver.ResolveLink(url);
            }
            return new Link
            {
                Url = url,
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
            if (!url.StartsWith("/"))
            {
                url = "/" + url;
            }
            return url;
        }

        protected virtual string GetPageContent(string url, Localization localization)
        {
            string cmUrl = GetCmUrl(url);

            using (new Tracer(url, cmUrl))
            {
                IPageFactory pageFactory = DD4TFactoryCache.GetPageFactory(localization);
                string result;
                pageFactory.TryFindPageContent(GetCmUrl(url), out result);
                return result;
            }
        }

        protected virtual IPage GetPage(string url, Localization localization)
        {
            string cmUrl = GetCmUrl(url);

            using (new Tracer(url, cmUrl))
            {
                IPageFactory pageFactory = DD4TFactoryCache.GetPageFactory(localization);
                IPage result;
                pageFactory.TryFindPage(cmUrl, out result);
                return result;
            }
        }
        
        protected virtual int MapSchema(string schemaKey, Localization localization)
        {
            string[] schemaKeyParts = schemaKey.Split('.');
            string moduleName = schemaKeyParts.Length > 1 ? schemaKeyParts[0] : SiteConfiguration.CoreModuleName;
            schemaKey = schemaKeyParts.Length > 1 ? schemaKeyParts[1] : schemaKeyParts[0];
            string schemaId = localization.GetConfigValue(string.Format("{0}.schemas.{1}", moduleName, schemaKey));

            int result;
            Int32.TryParse(schemaId, out result);
            return result;
        }

        protected virtual IEnumerable<IPage> GetIncludesFromModel(IPage page, Localization localization)
        {
            List<IPage> result = new List<IPage>();
            string[] pageTemplateTcmUriParts = page.PageTemplate.Id.Split('-');
            IEnumerable<string> includePageUrls = SiteConfiguration.GetIncludePageUrls(pageTemplateTcmUriParts[1], localization);
            foreach (string includePageUrl in includePageUrls)
            {
                IPage includePage = GetPage(SiteConfiguration.LocalizeUrl(includePageUrl, localization), localization);
                if (includePage == null)
                {
                    Log.Error("Include Page '{0}' not found.", includePageUrl);
                    continue;
                }
                FullyLoadDynamicComponentPresentations(includePage, localization);
                result.Add(includePage);
            }
            return result;
        }

        /// <summary>
        /// Ensures that the Component Fields of DCPs on the Page are populated.
        /// </summary>
        private static void FullyLoadDynamicComponentPresentations(IPage page, Localization localization)
        {
            using (new Tracer(page, localization))
            {
                foreach (ComponentPresentation dcp in page.ComponentPresentations.Where(cp => cp.IsDynamic).OfType<ComponentPresentation>())
                {
                    IComponentFactory componentFactory = DD4TFactoryCache.GetComponentFactory(localization);
                    dcp.Component = (Component)componentFactory.GetComponent(dcp.Component.Id, dcp.ComponentTemplate.Id);
                }
            }
        }
    }
}
