using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models.Entity
{
    /// <summary>
    /// Represents a special kind of <see cref="SitemapItem"/> which is used for Taxonomy Nodes.
    /// </summary>
    public class TaxonomyNode : SitemapItem
    {
        public string Key { get; set; }
        public string Description { get; set; }
        public List<string> RelatedTaxonomyNodeIds { get; set; }
        public bool IsAbstract { get; set; }
        public bool HasChildNodes { get; set; }
        public int ClassifiedItemsCount { get; set; }
        public IDictionary<string, object> CustomMetadata { get; set; }

        /// <summary>
        /// Creates a <see cref="Link"/> out of this <see cref="SitemapItem"/>.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/></param>
        /// <returns>The <see cref="Link"/> Entity Model or <c>null</c> if the <see cref="Url"/> property is <c>null</c> or empty.</returns>
        public override Link CreateLink(Localization localization)
        {
            Link result = base.CreateLink(localization);
            if (result != null)
            {
                result.AlternateText = Description;
            }
            return result;
        }
    }
}
