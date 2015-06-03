using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
#pragma warning disable 618
    /// <summary>
    /// Legacy Model for the data that is used to render a web page.
    /// </summary>
    [Obsolete("Deprecated in DXA 1.1. Use class PageModel instead.")]
    public class WebPage : PageBase
    {
        private readonly Dictionary<string, IPage> _includes = new Dictionary<string, IPage>(); 

        /// <summary>
        /// Gets or sets the URL of the Page.
        /// </summary>
        public string Url
        {
            get; 
            set;
        }

        /// <summary>
        /// Gets or sets the Page metadata which is typically rendered as HTML meta tags (name/value pairs).
        /// </summary>
        public IDictionary<string, string> Meta
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the Page Includes. The dictionary keys are the include names.
        /// </summary>
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
    }
#pragma warning restore 618
}