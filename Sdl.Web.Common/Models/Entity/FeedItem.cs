using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Intermediary between entity models and syndication objects for web feeds.
    /// </summary>
    public class FeedItem
    {
        /// <summary>
        /// Feed item title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Feed item summary.
        /// </summary>
        public RichText Summary { get; set; }

        /// <summary>
        /// Feed item link
        /// </summary>
        public Link Link { get; set; }

        /// <summary>
        /// Feed item update date.
        /// </summary>
        public DateTime? Date { get; set; }
    }
}
