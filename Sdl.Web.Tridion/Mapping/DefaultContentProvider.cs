using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
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
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Default Content Provider implementation (DD4T-based).
    /// </summary>
    public class DefaultContentProvider : IContentProvider, IRawDataProvider
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
        /// <param name="urlPath">The URL.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        [Obsolete("Deprecated in DXA 1.1. Use the overload that has a Localization parameter.")]
        public PageModel GetPageModel(string urlPath, bool addIncludes = true)
        {
            return GetPageModel(urlPath, WebRequestContext.Localization, addIncludes);
        }

        /// <summary>
        /// Populates a Dynamic List by executing the query it specifies.
        /// </summary>
        /// <param name="dynamicList">The Dynamic List which specifies the query and is to be populated.</param>
        [Obsolete("Deprecated in DXA 1.1. Use the overload that has a Localization parameter.")]
        public DynamicList PopulateDynamicList(DynamicList dynamicList)
        {
            PopulateDynamicList(dynamicList, WebRequestContext.Localization);
            return dynamicList;
        }

#pragma warning restore 618


        /// <summary>
        /// Gets a Page Model for a given URL.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context Localization.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given URL.</exception>
        public virtual PageModel GetPageModel(string urlPath, Localization localization, bool addIncludes)
        {        
            using (new Tracer(urlPath, localization, addIncludes))
            {
                //We can have a couple of tries to get the page model if there is no file extension on the url request, but it does not end in a slash:
                //1. Try adding the default extension, so /news becomes /news.html
                IPage page = GetPage(urlPath, localization);
                if (page == null && (urlPath == null || (!urlPath.EndsWith("/") && urlPath.LastIndexOf(".", StringComparison.Ordinal) <= urlPath.LastIndexOf("/", StringComparison.Ordinal))))
                {
                    //2. Try adding the default page, so /news becomes /news/index.html
                    page = GetPage(urlPath + "/", localization);
                }
                if (page == null)
                {
                    throw new DxaItemNotFoundException(urlPath, localization.LocalizationId);
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

                EntityModel result = ModelBuilderPipeline.CreateEntityModel(dcp, localization);
                if (result.XpmMetadata != null)
                {
                    // Entity Models requested through this method are per definition "query based" in XPM terminology.
                    result.XpmMetadata["IsQueryBased"] = true;
                }
                return result;
            }
        }

        /// <summary>
        /// Gets a Static Content Item for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
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
        /// Populates a Dynamic List by executing the query it specifies.
        /// </summary>
        /// <param name="dynamicList">The Dynamic List which specifies the query and is to be populated.</param>
        /// <param name="localization">The context Localization.</param>
        public virtual void PopulateDynamicList(DynamicList dynamicList, Localization localization)
        {
            using (new Tracer(dynamicList, localization))
            {
                Common.Models.Query query = dynamicList.GetQuery(localization);
                if (query == null || !(query is SimpleBrokerQuery))
                {
                    throw new DxaException(string.Format("Unexpected result from {0}.GetQuery: {1}", dynamicList.GetType().Name, query));
                }
                BrokerQuery brokerQuery = new BrokerQuery((SimpleBrokerQuery) query);
                dynamicList.QueryResults = brokerQuery.ExecuteQuery(dynamicList.ResultType).ToList();
                dynamicList.HasMore = brokerQuery.HasMore;
            }
        }

        #endregion

        #region IRawDataProvider members
        public virtual string GetPageContent(string urlPath, Localization localization)
        {
            string cmUrl = GetCmUrl(urlPath);

            using (new Tracer(urlPath, cmUrl))
            {
                IPageFactory pageFactory = DD4TFactoryCache.GetPageFactory(localization);
                string result;
                pageFactory.TryFindPageContent(GetCmUrl(urlPath), out result);
                return result;
            }
        }
        #endregion

        /// <summary>
        /// Converts a request URL path into a CMS URL (for example adding default page name and file extension)
        /// </summary>
        /// <param name="urlPath">The request URL path (unescaped)</param>
        /// <returns>A CMS URL (UTF-8 URL escaped)</returns>
        protected virtual string GetCmUrl(string urlPath)
        {
            string cmUrl;
            if (String.IsNullOrEmpty(urlPath))
            {
                cmUrl = Constants.DefaultPageName;
            }
            else
            {
                cmUrl = Uri.EscapeUriString(urlPath);
            }

            if (cmUrl.EndsWith("/"))
            {
                cmUrl = cmUrl + Constants.DefaultPageName;
            }
            if (!Path.HasExtension(cmUrl))
            {
                cmUrl = cmUrl + Constants.DefaultExtension;
            }
            if (!cmUrl.StartsWith("/"))
            {
                cmUrl = "/" + cmUrl;
            }
            return cmUrl;
        }

        protected virtual IPage GetPage(string urlPath, Localization localization)
        {
            string cmUrl = GetCmUrl(urlPath);

            using (new Tracer(urlPath, cmUrl))
            {
                IPageFactory pageFactory = DD4TFactoryCache.GetPageFactory(localization);
                IPage result;
                pageFactory.TryFindPage(cmUrl, out result);
                return result;
            }
        }

        protected virtual IEnumerable<IPage> GetIncludesFromModel(IPage page, Localization localization)
        {
            List<IPage> result = new List<IPage>();
            string[] pageTemplateTcmUriParts = page.PageTemplate.Id.Split('-');
            IEnumerable<string> includePageUrls = localization.GetIncludePageUrls(pageTemplateTcmUriParts[1]);
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
