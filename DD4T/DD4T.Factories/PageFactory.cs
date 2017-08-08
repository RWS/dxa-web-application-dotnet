using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Exceptions;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.Utils;
using DD4T.ContentModel.Factories;
using DD4T.Serialization;
using DD4T.ContentModel.Contracts.Serializing;
using DD4T.Utils.Caching;

namespace DD4T.Factories
{
    /// <summary>
    ///
    /// </summary>
    public class PageFactory : FactoryBase, IPageFactory
    {
        private static IDictionary<string, DateTime> lastPublishedDates = new Dictionary<string, DateTime>();
        private static Regex rePageContentByUrl = new Regex("PageContent_([0-9\\-]+)_(.*)");

        private const string CacheValueNullTitle = "DD4T-Special-Value-PageNotFound";
        private IPage CacheValueNull;

        //private ICacheAgent _cacheAgent = null;
        public const string CacheRegion = "Page";

        public const string CacheRegion404 = "Page404";
        public IPageProvider PageProvider { get; set; }
        public ILinkFactory LinkFactory { get; set; }
        public IComponentPresentationFactory ComponentPresentationFactory { get; set; }

        public PageFactory(IPageProvider pageProvider, IComponentPresentationFactory componentPresentationFactory,
                            IFactoryCommonServices factoryCommonServices)
            : base(factoryCommonServices)
        {
            if (pageProvider == null)
                throw new ArgumentNullException("pageProvider");

            if (componentPresentationFactory == null)
                throw new ArgumentNullException("componentPresentationFactory");

            ComponentPresentationFactory = componentPresentationFactory;
            PageProvider = pageProvider;
            CacheValueNull = new Page() { Title = CacheValueNullTitle };
        }

        private string DataFormat
        {
            get
            {
                return Configuration.DataFormat;
            }
        }

        private ISerializerService _serializerService;

        private ISerializerService SerializerService
        {
            get
            {
                if (_serializerService == null)
                {
                    if (DataFormat.ToLower() == "json")
                    {
                        _serializerService = new JSONSerializerService();
                    }
                    if (DataFormat.ToLower() == "xml")
                    {
                        _serializerService = new XmlSerializerService();
                    }
                    _serializerService = new AutoDetectSerializerService();
                }
                return _serializerService;
            }
        }

        #region IPageFactory Members

        public virtual bool TryFindPage(string url, out IPage page)
        {
            LoggerService.Debug(">>TryFindPage ({0}", LoggingCategory.Performance, url);
            page = null;

            string cacheKey = CacheKeyFactory.GenerateKey(CacheRegion, url, Convert.ToString(PublicationId));

            LoggerService.Debug("about to load page from cache with key {0}", LoggingCategory.Performance, cacheKey);
            page = (IPage)CacheAgent.Load(cacheKey);
            LoggerService.Debug("finished loading page from cache with key {0}, page found = {1}", LoggingCategory.Performance, cacheKey, Convert.ToString(page != null));

            if (page != null)
            {
                if (page.Title == CacheValueNullTitle)
                {
                    page = null;
                    return false;
                }

                LoggerService.Debug("<<TryFindPage ({0}", LoggingCategory.Performance, url);
                return true;
            }
            else
            {
                LoggerService.Debug("about to load page content from provider with url {0}", LoggingCategory.Performance, url);
                string pageContentFromBroker = PageProvider.GetContentByUrl(url);
                LoggerService.Debug("finished loading page content from provider with url {0}, has value: {1}", LoggingCategory.Performance, url, Convert.ToString(!(string.IsNullOrEmpty(pageContentFromBroker))));

                if (string.IsNullOrEmpty(pageContentFromBroker))
                {
                    CacheAgent.Store(cacheKey, CacheRegion404, CacheValueNull);
                }
                else
                {
                    LoggerService.Debug("about to create IPage from content for url {0}", LoggingCategory.Performance, url);
                    page = GetIPageObject(pageContentFromBroker);
                    if (IncludeLastPublishedDate)
                        ((Page)page).LastPublishedDate = PageProvider.GetLastPublishedDateByUrl(url);
                    LoggerService.Debug("finished creating IPage from content for url {0}", LoggingCategory.Performance, url);
                    LoggerService.Debug("about to store page in cache with key {0}", LoggingCategory.Performance, cacheKey);
                    CacheAgent.Store(cacheKey, CacheRegion, page, new List<string> { page.Id });
                    LoggerService.Debug("finished storing page in cache with key {0}", LoggingCategory.Performance, cacheKey);
                    LoggerService.Debug("<<TryFindPage ({0}", LoggingCategory.Performance, url);
                    return true;
                }
            }

            LoggerService.Debug("<<TryFindPage ({0}", LoggingCategory.Performance, url);
            return false;
        }

        public IPage FindPage(string url)
        {
            IPage page;
            if (!TryFindPage(url, out page))
            {
                throw new PageNotFoundException();
            }
            return page;
        }

        public virtual bool TryFindPageContent(string url, out string pageContent)
        {
            LoggerService.Debug(">>TryFindPageContent ({0}", LoggingCategory.Performance, url);

            pageContent = string.Empty;

            string cacheKey = String.Format("PageContent_{0}_{1}", PublicationId, url);

            LoggerService.Debug("about to load page content from cache with key {0}", LoggingCategory.Performance, cacheKey);
            pageContent = (string)CacheAgent.Load(cacheKey);
            LoggerService.Debug("finished loading page content from cache with key {0}, pageContent found {1}", LoggingCategory.Performance, cacheKey, Convert.ToString(!(string.IsNullOrEmpty(pageContent))));
            if (pageContent != null)
            {
                LoggerService.Debug("<<TryFindPageContent ({0}", LoggingCategory.Performance, url);
                return true;
            }
            else
            {
                LoggerService.Debug("about to load page content from provider with url {0}", LoggingCategory.Performance, url);
                string tempPageContent = PageProvider.GetContentByUrl(url);
                LoggerService.Debug("finished loading page content from provider with url {0}, has value: {1}", LoggingCategory.Performance, url, Convert.ToString(!(string.IsNullOrEmpty(tempPageContent))));
                if (!string.IsNullOrEmpty(tempPageContent))
                {
                    pageContent = tempPageContent;
                    LoggerService.Debug("about to store page in cache with key {0}", LoggingCategory.Performance, cacheKey);
                    CacheAgent.Store(cacheKey, CacheRegion, pageContent);
                    LoggerService.Debug("finished storing page in cache with key {0}", LoggingCategory.Performance, cacheKey);
                    LoggerService.Debug("<<TryFindPageContent ({0}", LoggingCategory.Performance, url);
                    return true;
                }
            }

            LoggerService.Debug("<<TryFindPageContent ({0}", LoggingCategory.Performance, url);
            return false;
        }

        public string FindPageContent(string url)
        {
            string pageContent;
            if (!TryFindPageContent(url, out pageContent))
            {
                throw new PageNotFoundException();
            }

            return pageContent;
        }

        public bool TryGetPage(string tcmUri, out IPage page)
        {
            page = null;

            string cacheKey = String.Format("PageByUri_{0}", tcmUri);

            page = (IPage)CacheAgent.Load(cacheKey);
            if (page != null)
            {
                return true;
            }
            string tempPageContent = PageProvider.GetContentByUri(tcmUri);
            if (tempPageContent != string.Empty)
            {
                page = GetIPageObject(tempPageContent);
                if (IncludeLastPublishedDate)
                    ((Page)page).LastPublishedDate = PageProvider.GetLastPublishedDateByUri(tcmUri);

                CacheAgent.Store(cacheKey, CacheRegion, page);

                return true;
            }

            return false;
        }

        public IPage GetPage(string tcmUri)
        {
            IPage page;
            if (!TryGetPage(tcmUri, out page))
            {
                throw new PageNotFoundException();
            }

            return page;
        }

        public bool TryGetPageContent(string tcmUri, out string pageContent)
        {
            pageContent = string.Empty;

            string cacheKey = String.Format("PageContentByUri_{0}", tcmUri);
            pageContent = (string)CacheAgent.Load(cacheKey);
            if (pageContent != null)
            {
                return true;
            }
            else
            {
                string tempPageContent = PageProvider.GetContentByUri(tcmUri);
                if (tempPageContent != string.Empty)
                {
                    pageContent = tempPageContent;
                    CacheAgent.Store(cacheKey, CacheRegion, pageContent);
                    return true;
                }
            }

            return false;
        }

        public string GetPageContent(string tcmUri)
        {
            string pageContent;
            if (!TryGetPageContent(tcmUri, out pageContent))
            {
                throw new PageNotFoundException();
            }

            return pageContent;
        }

        public bool HasPageChanged(string url)
        {
            return true; // TODO: implement
        }

        /// <summary>
        /// Returns an IPage object
        /// </summary>
        /// <param name="pageStringContent">String to desirialize to an IPage object</param>
        /// <returns>IPage object</returns>
        public IPage GetIPageObject(string pageStringContent)
        {
            LoggerService.Debug(">>GetIPageObject(string length {0})", LoggingCategory.Performance, Convert.ToString(pageStringContent.Length));

            LoggerService.Debug("GetIPageObject: about to deserialize", LoggingCategory.Performance);
            IPage page = SerializerService.Deserialize<Page>(pageStringContent);
            LoggerService.Debug("GetIPageObject: finished deserializing", LoggingCategory.Performance);

            // set order on page for each ComponentPresentation
            int orderOnPage = 0;
            foreach (IComponentPresentation cp in page.ComponentPresentations)
            {
                cp.OrderOnPage = orderOnPage++;
            }
            LoggerService.Debug("GetIPageObject: about to load DCPs", LoggingCategory.Performance);
            LoadComponentModelsFromComponentPresentationFactory(page);
            LoggerService.Debug("GetIPageObject: finished loading DCPs", LoggingCategory.Performance);
            LoggerService.Debug("<<GetIPageObject(string length {0})", LoggingCategory.Performance, Convert.ToString(pageStringContent.Length));
            return page;
        }

        public DateTime GetLastPublishedDateByUrl(string url)
        {
            LoggerService.Debug("about to retrieve last published date from provider for url {0}", LoggingCategory.Performance, url);
            DateTime dt = PageProvider.GetLastPublishedDateByUrl(url);
            LoggerService.Debug("finished retrieving last published date from provider for url {0}", LoggingCategory.Performance, url);
            return dt;
        }

        public DateTime GetLastPublishedDateByUri(string uri)
        {
            return PageProvider.GetLastPublishedDateByUri(uri);
        }

        [Obsolete]
        public override DateTime GetLastPublishedDateCallBack(string key, object cachedItem)
        {
            LoggerService.Debug(">>GetLastPublishedDateCallBack {0}", LoggingCategory.Performance, key);
            if (cachedItem == null)
                return DateTime.Now; // this will force the item to be removed from the cache
            if (cachedItem is IPage)
            {
                DateTime dt = GetLastPublishedDateByUri(((IPage)cachedItem).Id);
                LoggerService.Debug("<<GetLastPublishedDateCallBack {0}", LoggingCategory.Performance, key);
                return dt;
            }

            Match m = rePageContentByUrl.Match(key);
            if (m.Success)
            {
                int publicationId = Convert.ToInt32(m.Groups[1].Value);
                if (publicationId == Int32.MinValue)
                    publicationId = 0;
                string url = m.Groups[2].Value;
                DateTime dt = GetLastPublishedDateByUrl(url);
                LoggerService.Debug("<<GetLastPublishedDateCallBack {0} -- regex", LoggingCategory.Performance, key);
                return dt;
            }

            if (key.StartsWith("PageContentByUri_"))
            {
                string uri = key.Substring("PageContentByUri_".Length);
                DateTime dt = GetLastPublishedDateByUri(uri);
                LoggerService.Debug("<<GetLastPublishedDateCallBack {0} -- 'PageContentByUri_'", LoggingCategory.Performance, key);
                return dt;
            }

            throw new Exception(string.Format("GetLastPublishedDateCallBack called for unexpected object type '{0}' or with unexpected key '{1}'", cachedItem.GetType(), key));
        }

        public string[] GetAllPublishedPageUrls(string[] includeExtensions, string[] pathStarts)
        {
            return PageProvider.GetAllPublishedPageUrls(includeExtensions, pathStarts);
        }

        #endregion IPageFactory Members

        #region private helper methods

        private void LoadComponentModelsFromComponentPresentationFactory(IPage page)
        {
            LoggerService.Debug(">>LoadComponentModelsFromComponentPresentationFactory ({0})", LoggingCategory.Performance, page.Id);

            foreach (DD4T.ContentModel.ComponentPresentation cp in page.ComponentPresentations)
            {
                if (cp.Component == null)
                    continue;

                // added by QS: only load DCPs from broker if they are in fact dynamic!
                if (cp.IsDynamic)
                {
                    try
                    {
                        ComponentPresentation dcp = (ComponentPresentation)ComponentPresentationFactory.GetComponentPresentation(cp.Component.Id, cp.ComponentTemplate.Id);
                        cp.Component = dcp.Component;
                        //Fix for backward compatibility. We keep using the template on the page as 1.x version of dd4t doesn't have the componenttemplate data in the dcp
                        //The title is mandatory in tridion, so it can't be null or empty when successful, the id will still be set by the factory
                        if (!String.IsNullOrEmpty(dcp.ComponentTemplate.Title))
                        {
                            cp.ComponentTemplate = dcp.ComponentTemplate;
                        }
                        cp.TargetGroupConditions = dcp.TargetGroupConditions;
                    }
                    catch (ComponentPresentationNotFoundException)
                    {
                        LoggerService.Warning("dynamic component presentation {0} / {1} included on page {2} cannot be found; it may have been unpublished", LoggingCategory.Model, cp.Component.Id, cp.ComponentTemplate.Id, page.Id);
                        // DCPs can be unpublished while the page that they are embedded on remain published
                        // in this case, simply ignore the exception (the 'stub' component is still on the page)
                    }
                }
            }
            LoggerService.Debug("<<LoadComponentModelsFromComponentPresentationFactory ({0})", LoggingCategory.Performance, page.Id);
        }

        //private void resolveLinks(Field richTextField, TcmUri pageUri)
        //{
        //    // Find any <a> nodes with xlink:href="tcm attribute
        //    string nodeText = richTextField.Value;
        //    XmlDocument tempDocument = new XmlDocument();
        //    tempDocument.LoadXml("<tempRoot>" + nodeText + "</tempRoot>");

        //    XmlNamespaceManager nsManager = new XmlNamespaceManager(tempDocument.NameTable);
        //    nsManager.AddNamespace("xlink", "http://www.w3.org/1999/xlink");

        //    XmlNodeList linkNodes = tempDocument.SelectNodes("//*[local-name()='a'][@xlink:href[starts-with(string(.),'tcm:')]]", nsManager);

        //    foreach (XmlNode linkElement in linkNodes)
        //    {
        //        // TODO test with including the Page Uri, seems to always link with Source Page
        //        //string linkText = linkFactory.ResolveLink(pageUri.ToString(), linkElement.Attributes["xlink:href"].Value, "tcm:0-0-0");
        //        string linkText = LinkFactory.ResolveLink("tcm:0-0-0", linkElement.Attributes["xlink:href"].Value, "tcm:0-0-0");
        //        if (!string.IsNullOrEmpty(linkText))
        //        {
        //            XmlAttribute linkUrl = tempDocument.CreateAttribute("href");
        //            linkUrl.Value = linkText;
        //            linkElement.Attributes.Append(linkUrl);

        //            // Remove the other xlink attributes from the a element
        //            for (int attribCount = linkElement.Attributes.Count - 1; attribCount >= 0; attribCount--)
        //            {
        //                if (!string.IsNullOrEmpty(linkElement.Attributes[attribCount].NamespaceURI))
        //                {
        //                    linkElement.Attributes.RemoveAt(attribCount);
        //                }
        //            }
        //        }
        //    }

        //    if (linkNodes.Count > 0)
        //    {
        //        richTextField.Values.Clear();
        //        richTextField.Values.Add(tempDocument.DocumentElement.InnerXml);
        //    }
        //}

        #endregion private helper methods
    }
}