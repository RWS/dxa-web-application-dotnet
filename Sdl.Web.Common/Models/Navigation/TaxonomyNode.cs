using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using System;

namespace Sdl.Web.Common.Models.Navigation
{
    /// <summary>
    /// Represents a special kind of <see cref="SitemapItem"/> which is used for Taxonomy Nodes.
    /// </summary>
    [Serializable]
    public class TaxonomyNode : SitemapItem
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public bool IsAbstract { get; set; }
        public bool HasChildNodes { get; set; }
        public int ClassifiedItemsCount { get; set; }

        /// <summary>
        /// Creates a <see cref="Link"/> out of this <see cref="SitemapItem"/>.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/></param>
        /// <returns>The <see cref="Link"/> Entity Model.</returns>
        public override Link CreateLink(Localization localization)
        {
            Link result = base.CreateLink(localization);
            result.AlternateText = Description;
            return result;
        }
    }
}
