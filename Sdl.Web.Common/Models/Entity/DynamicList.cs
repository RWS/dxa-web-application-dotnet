using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for Entity Models which get populated dynamically.
    /// </summary>
    public abstract class DynamicList : EntityModel, ISyndicationFeedItemProvider
    {
        protected DynamicList()
        {
            QueryResults = new List<EntityModel>();
        }

        public int Start { get; set; }

        public bool HasMore { get; set; }

        [JsonIgnore]
        [SemanticProperty(ignoreMapping: true)]
        public List<EntityModel> QueryResults
        {
            get;
            set;
        }
    
        public abstract Query GetQuery(Localization localization);

        [JsonIgnore]
        public abstract Type ResultType { get; }

        #region ISyndicationFeedItemProvider members
        /// <summary>
        /// Extracts syndication feed items.
        /// </summary>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <returns>The extracted syndication feed items; a concatentation of syndication feed items provided by <see cref="QueryResults"/> (if any).</returns>
        public virtual IEnumerable<SyndicationItem> ExtractSyndicationFeedItems(Localization localization)
        {
            return ConcatenateSyndicationFeedItems(QueryResults.OfType<ISyndicationFeedItemProvider>(), localization);
        }
        #endregion
    }
}
