namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents the View Model for a Page
    /// </summary>
#pragma warning disable 618
    // TODO DXA 2.0: Should inherit directly from ViewModel, but for now we need the legacy classes inbetween for compatibility.
    public class PageModel : WebPage
#pragma warning restore 618
    {
        /// <summary>
        /// Gets the Page Regions.
        /// </summary>
        public new RegionModelSet Regions
        {
            get
            {
                return _regions;
            }
        }

        /// <summary>
        /// Initializes a new instance of PageModel.
        /// </summary>
        /// <param name="id">The identifier of the Page.</param>
        public PageModel(string id)
            : base(id)
        {
        }
    }

}
