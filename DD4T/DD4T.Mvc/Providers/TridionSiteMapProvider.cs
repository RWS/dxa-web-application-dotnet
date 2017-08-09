namespace DD4T.Mvc.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;
    using System.Web;
    using System.Xml.Linq;
    using System.Collections;
    using DD4T.Factories;
    using DD4T.ContentModel.Contracts.Caching;
    using DD4T.Factories.Caching;
    using DD4T.ContentModel.Contracts.Logging;
    using DD4T.ContentModel.Factories;
    using DD4T.ContentModel.Contracts.Configuration;

    public class TridionSiteMapProvider : StaticSiteMapProvider
    {

        private int debugCounter = 0;

        // Sitemap attributes which are not considered part of route, you can add your custom attributes here.
        private string[] _excludedAttributes = { "title", "description", "roles", "page", "topmenuposition" };


        public const string CacheRegion = "System";
        public const string DefaultCacheKey = "SiteMapRootNode";
        public const string CacheNullValue = "NULL";
        public const string DefaultSiteMapPath = "/system/sitemap/sitemap.xml";

        private bool ShouldResolveComponentLinks { get; set; }

        private readonly IPageFactory _pageFactory;
        private readonly ILogger _logger;
        private readonly IDD4TConfiguration _configuration;
        private readonly ICacheAgent _cacheAgent;


        public TridionSiteMapProvider(IPageFactory pageFactory, ICacheAgent cacheAgent, ILogger logger, IDD4TConfiguration configuration)
        {
            if (pageFactory == null)
                throw new ArgumentNullException("pageFactory");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (configuration == null)
                throw new ArgumentNullException("configuration");
            if (cacheAgent == null)
                throw new ArgumentNullException("cacheAgent");

            _pageFactory = pageFactory;
            _configuration = configuration;
            _cacheAgent = cacheAgent;
            _logger = logger;
        }

        public virtual ILinkFactory LinkFactory { get; set; }
        public Dictionary<string, SiteMapNode> NodeDictionary { get; set; }
        public virtual string SiteMapPath
        {
            get
            {
                string path = _configuration.SiteMapPath;
                if (string.IsNullOrEmpty(path))
                {
                    return DefaultSiteMapPath;
                }
                return path;
            }
        }


        private SiteMapNode ReadSitemapFromXml(string sitemapUrl)
        {
            _logger.Debug(">>ReadSitemapFromXml", LoggingCategory.Performance);
            SiteMapNode rootNode = null;
            NodeDictionary = new Dictionary<string, SiteMapNode>();

            string sitemap;
            if (!_pageFactory.TryFindPageContent(sitemapUrl, out sitemap))
            {
                sitemap = emptySiteMapString();
            }
            _logger.Debug(string.Format("loaded sitemap with url {0}, length {1}", sitemapUrl, sitemap.Length), LoggingCategory.Performance);

            XDocument xDoc = XDocument.Parse(sitemap);

            _logger.Debug("parsed sitemap into XDocument", LoggingCategory.Performance);

            //XElement siteMapRoot = xDoc.Element("siteMap");
            XElement siteMapRoot = xDoc.Root;


            try
            {
                rootNode = new TridionSiteMapNode(this, String.Empty, "root_" + _pageFactory.PageProvider.PublicationId, String.Empty, String.Empty, String.Empty, new ArrayList(), new NameValueCollection(), new NameValueCollection(), String.Empty);
                _logger.Debug("created root node", LoggingCategory.Performance);
                AddNode(rootNode);
                _logger.Debug("added root node", LoggingCategory.Performance);

                //Fill down the hierarchy.
                AddChildren(rootNode, siteMapRoot.Elements(), 1);
            }
            catch (Exception e)
            {
                Exception e2 = e;
            }
            _logger.Debug("<<ReadSitemapFromXml", LoggingCategory.Performance);
            return rootNode;
        }

        //protected override void AddNode(SiteMapNode node, SiteMapNode parentNode)
        //{
        //    parentNode.ChildNodes.Add(node);
        //}

        private void AddChildren(SiteMapNode rootNode, IEnumerable<XElement> siteMapNodes, int currentLevel)
        {
            _logger.Debug(">>AddChildren for root node {0} at level {1}", LoggingCategory.Performance, rootNode.Title, currentLevel);
            foreach (var element in siteMapNodes)
            {
                _logger.Debug(">>>for loop iteration: {0}", LoggingCategory.Performance, element.ToString());
                SiteMapNode childNode = CreateNodeFromElement(element, currentLevel);
                //childNode = new TridionSiteMapNode(this,
                //    element.Attribute("id").Value, //key
                //    uri,
                //    element.Attribute("url").Value, //url
                //    element.Attribute("title").Value, //title
                //    element.Attribute("description").Value, //description
                //    null, //roles
                //    attributes, //attributes
                //    null, //explicitresourceKeys
                //    null) { Level = currentLevel }; // implicitresourceKey
                _logger.Debug("finished creating TridionSiteMapNode", LoggingCategory.Performance);

                _logger.Debug("about to add TridionSiteMapNode to node dictionary", LoggingCategory.Performance);
                _logger.Debug("finished adding TridionSiteMapNode to node dictionary", LoggingCategory.Performance);

                //Use the SiteMapNode AddNode method to add the SiteMapNode to the ChildNodes collection
                _logger.Debug("about to add node to SiteMap", LoggingCategory.Performance);
                AddNode(childNode, rootNode);
                _logger.Debug(string.Format("finished adding node to sitemap (title={0}, parent title={1})", childNode.Title, rootNode.Title), LoggingCategory.Performance);

                // Check for children in this node.
                AddChildren(childNode, element.Elements(), currentLevel + 1);
                _logger.Debug("<<<for loop iteration: {0}", LoggingCategory.Performance, element.ToString());
            }

            _logger.Debug("<<AddChildren for root node {0} at level {1}", LoggingCategory.Performance, rootNode.Title, currentLevel);
        }

        protected virtual SiteMapNode CreateNodeFromElement(XElement element, int currentLevel)
        {
            var attributes = new NameValueCollection();
            foreach (var a in element.Attributes())
            {
                attributes.Add(a.Name.ToString(), a.Value);
            }

            string uri;
            try
            {
                if (element.Attribute("uri") != null)
                    uri = element.Attribute("uri").Value;
                else if (element.Attribute("pageId") != null)
                    uri = element.Attribute("pageId").Value;
                else
                    uri = "";
            }
            catch
            {
                _logger.Debug("exception while retrieving uri", LoggingCategory.General);
                uri = "";
            }
            SiteMapNode childNode = new TridionSiteMapNode(this,
                element.Attribute("id").Value, //key
                uri,
                element.Attribute("url").Value, //url
                element.Attribute("title").Value, //title
                element.Attribute("description").Value, //description
                null, //roles
                attributes, //attributes
                null, //explicitresourceKeys
                null) { Level = currentLevel }; // implicitresourceKey

            NodeDictionary.Add(childNode.Key, childNode);

            return childNode;
        }

        public virtual bool IsInitialized
        {
            get;
            private set;
        }

        public override void Initialize(string name, NameValueCollection attributes)
        {
            if (!IsInitialized)
            {
                base.Initialize(name, attributes);
                IsInitialized = true;
            }
        }

        public virtual string CacheKey
        {
            get
            {
                return DefaultCacheKey;
            }
        }



        private object lock1 = new object();
        public override SiteMapNode BuildSiteMap()
        {
            debugCounter++;
            object result = _cacheAgent.Load(CacheKey);
            if (result != null)
            {
                if (result is string && ((string)result).Equals(CacheNullValue))
                    return null;
                return result as SiteMapNode;
            }
            SiteMapNode rootNode;
            lock (lock1)
            {
                base.Clear();
                rootNode = ReadSitemapFromXml(SiteMapPath);
                if (rootNode == null)
                {
                    // cache special 'null value' (so we do not try to load the sitemap from an invalid or non-existing XML every time!)
                    _cacheAgent.Store(CacheKey, CacheRegion, CacheNullValue);
                }
                else
                {
                    // Store the root node in the cache.
                    _cacheAgent.Store(CacheKey, CacheRegion, rootNode);
                }
            }

            return rootNode;
        }

        private string emptySiteMapString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<siteMap publicationid=\"tcm:0-70-1\">");
            sb.Append("<siteMapNode title=\"website\" url=\"/\">");
            sb.Append("</siteMapNode>");
            sb.Append("</siteMap>");

            return sb.ToString();
        }

        protected override SiteMapNode GetRootNodeCore()
        {
            return BuildSiteMap();
        }

        public override SiteMapNode RootNode
        {
            get
            {
                return BuildSiteMap();
            }
        }

        protected override void Clear()
        {
            lock (this)
            {
                // CacheAgent.Remove("rootNode"); // currently, CacheAgents do not support 'Remove'
                base.Clear();
            }
        }

    }
}
