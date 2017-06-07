using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents a node in the Navigation Model (site map).
    /// </summary>
    [Serializable]
    public class SitemapItem : EntityModel
    {
        /// <summary>
        /// Represents the possible values for <see cref="SitemapItem.Type"/>
        /// </summary>
        public class Types
        {
            public const string Page = "Page";
            public const string StructureGroup = "StructureGroup";
            public const string TaxonomyNode = "TaxonomyNode";
        }

        public SitemapItem()
        {
            Items = new List<SitemapItem>();
        }

        public SitemapItem(String title)
        {
            Items = new List<SitemapItem>();
            Title = title;
        }

        public string Title { get; set; }

        private Lazy<string> _url;
        public string Url 
        {
            get
            {
                return _url?.Value;
            }
            set
            {
                _url = new Lazy<string>(()=>ResolveUrl(value), true);
            }
        }

        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public DateTime? PublishedDate { get; set; } // NOTE: the type was changed from type string in DXA 1.6. It (de)serializes the same to/from JSON, though.
        public bool Visible { get; set; }

        [JsonIgnore]
        public SitemapItem Parent { get; set; }

        [JsonIgnore]
        public string OriginalTitle { get; set; }

        [JsonProperty("OriginalTitle")]
        private string OriginalTitleSetter
        {
            set { OriginalTitle = value; }
        }

        /// <summary>
        /// Creates a <see cref="Link"/> out of this <see cref="SitemapItem"/>.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/></param>
        /// <returns>The <see cref="Link"/> Entity Model.</returns>
        public virtual Link CreateLink(Localization localization)
        {
            return new Link
            {
                Url = Url,
                LinkText = Title
            };
        }

        /// <summary>
        /// Finds a SitemapItem with a given URL path in the Navigation subtree rooted by this <see cref="SitemapItem"/>.
        /// </summary>
        /// <param name="urlPath">The URL path to search for.</param>
        /// <returns>The <see cref="SitemapItem"/> with the given URL path or <c>null</c> if no such item is found.</returns>
        public SitemapItem FindSitemapItem(string urlPath)
        {
            if (Url != null && Url.Equals(urlPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return this;
            }

            return Items?.Select(i => i.FindSitemapItem(urlPath)).FirstOrDefault(i => i != null);
        }

        /// <summary>
        /// Given a url if it represents a tcm item attempt to resolve to real url
        /// </summary>
        /// <param name="url">Url to attempt to resolve</param>
        /// <returns>Resolved url</returns>
        protected string ResolveUrl(string url)
        {
            if (url != null && url.StartsWith("tcm:"))
            {
                return SiteConfiguration.LinkResolver.ResolveLink(url);
            }
            return url;
        }
    }
}
