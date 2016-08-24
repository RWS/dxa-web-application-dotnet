using System;
using System.Collections.Generic;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents a node in the Navigation Model (site map).
    /// </summary>
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

        /// <summary>
        /// Creates a <see cref="Link"/> out of this <see cref="SitemapItem"/>.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/></param>
        /// <returns>The <see cref="Link"/> Entity Model or <c>null</c> if the <see cref="Url"/> property is <c>null</c> or empty.</returns>
        public virtual Link CreateLink(Localization localization)
        {
            string linkUrl = Url;
            if (string.IsNullOrEmpty(linkUrl))
            {
                return null;
            }
            if (linkUrl.StartsWith("tcm:"))
            {
                linkUrl = SiteConfiguration.LinkResolver.ResolveLink(linkUrl);
            }
            return new Link
            {
                Url = linkUrl,
                LinkText = Title
            };
        }
    }
}
