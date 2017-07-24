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
    [Serializable]
    public abstract class DynamicList : EntityModel, ISyndicationFeedItemProvider
    {
        protected DynamicList()
        {
            QueryResults = new List<EntityModel>();
        }

        [SemanticProperty(IgnoreMapping = true)]
        public int Start { get; set; }

        [SemanticProperty(IgnoreMapping = true)]
        public bool HasMore { get; set; }
        
        [SemanticProperty(IgnoreMapping = true)]
        public List<EntityModel> QueryResults { get; set; }

        public abstract Query GetQuery(Localization localization);

        [JsonIgnore]
        [SemanticProperty(IgnoreMapping = true)]
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

        #region Overrides
        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public override ViewModel DeepCopy()
        {
            DynamicList clone = (DynamicList) base.DeepCopy();
            if (QueryResults != null)
            {
                clone.QueryResults = new List<EntityModel>(QueryResults);
            }
            return clone;
        }
        #endregion
    }
}
