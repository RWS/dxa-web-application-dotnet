using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sdl.Web.Common.Models
{
#pragma warning disable 618
    /// <summary>
    /// Legacy Model for the data that is used to render a web page.
    /// </summary>
    [Obsolete("Deprecated in DXA 1.1. Use class PageModel instead.")]
    [Serializable]
    public abstract class WebPage : PageBase
    {
        private readonly Dictionary<string, IPage> _includes = new Dictionary<string, IPage>(); 

        /// <summary>
        /// Gets or sets the URL path of the Page.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Url
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets or sets the Page metadata which is typically rendered as HTML meta tags (name/value pairs).
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, string> Meta
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the Page Includes. The dictionary keys are the include names.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Page Includes are now modeled as Regions, so use PageModel.Regions instead.")]
        public Dictionary<string, IPage> Includes
        {
            get
            {
                return _includes;
            }
        }

        /// <summary>
        /// Initializes a new instance of WebPage
        /// </summary>
        /// <param name="id">The identifier for the Page.</param>
        protected WebPage(string id)
            : base(id)
        {
            Meta = new Dictionary<string, string>();
        }

        #region Overrides

        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public override ViewModel DeepCopy()
        {
            WebPage clone = (WebPage) base.DeepCopy();
            if (Meta != null)
            {
                clone.Meta = new Dictionary<string, string>(Meta);
            }
            return clone;
        }
        #endregion
    }
#pragma warning restore 618
}