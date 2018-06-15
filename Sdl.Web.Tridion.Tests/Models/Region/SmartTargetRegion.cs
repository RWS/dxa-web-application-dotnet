using System;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Tridion.Tests.Models
{
    /// <summary>
    /// View Model for SmartTarget Regions.
    /// </summary>
    [Serializable]
    public class SmartTargetRegion : RegionModel
    {
        public SmartTargetRegion(string name) 
            : base(name)
        {
        }

        public SmartTargetRegion(string name, string qualifiedViewName) 
            : base(name, qualifiedViewName)
        {
        }
        
        /// <summary>
        /// Indicates whether the Region has SmartTarget content (Promotions) or fallback content.
        /// </summary>
        public bool HasSmartTargetContent { get; set; }

        /// <summary>
        /// The maximum number of SmartTarget items to output in this Region.
        /// </summary>
        public int MaxItems { get; set; }

        /// <summary>
        /// Gets the Start Query XPM markup (for staging sites).
        /// </summary>
        /// <returns>The "Start Query" XPM markup if the site is a staging site. An empty string otherwise.</returns>
        /// <remarks>
        /// A SmartTarget Region has two pieces of XPM markup: a "Start Promotion Region" tag and a "Start Query" tag.
        /// The regular XPM markup mechanism (Html.DxaRegionMarkup()) renders the "Start Promotion Region" tag and this method
        /// should be called from the Region View code to render the "Start Query" tag in the right location.
        /// </remarks>
        public string GetStartQueryXpmMarkup()
        {
            return (XpmMetadata == null) ? String.Empty : (string) XpmMetadata["Query"];
        }

        /// <summary>
        /// Gets the XPM markup to be output by the Html.DxaRegionMarkup() method.
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public override string GetXpmMarkup(ILocalization localization)
        {
            return String.Format("<!-- Start Promotion Region: {{ \"RegionID\": \"{0}\"}} -->", Name);
        }
    }
}
