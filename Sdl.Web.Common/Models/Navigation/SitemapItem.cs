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
        public string Url { get; set; }
        public string Type { get; set; }
        public List<SitemapItem> Items { get; set; }
        public DateTime? PublishedDate { get; set; } // NOTE: the type was changed from type string in DXA 1.6. It (de)serializes the same to/from JSON, though.
        public bool Visible { get; set; }

        [JsonIgnore]
        public SitemapItem Parent { get; set; }

        [JsonIgnore]
        public string OriginalTitle { get; set; }

        /// <summary>
        /// Creates a <see cref="Link"/> out of this <see cref="SitemapItem"/>.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/></param>
        /// <returns>The <see cref="Link"/> Entity Model.</returns>
        public virtual Link CreateLink(Localization localization)
        {
            string linkUrl = Url;
            if (linkUrl != null && linkUrl.StartsWith("tcm:"))
            {
                linkUrl = SiteConfiguration.LinkResolver.ResolveLink(linkUrl);
            }
            return new Link
            {
                Url = linkUrl,
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

            return (Items == null) ? null : Items.Select(i => i.FindSitemapItem(urlPath)).FirstOrDefault(i => i != null);
        }

    }
}
