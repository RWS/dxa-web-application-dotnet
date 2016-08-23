using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public int ClassifiedItemsCount { get; set; }
        public IDictionary<string, object> CustomMetadata { get; set; } 
    }
}
