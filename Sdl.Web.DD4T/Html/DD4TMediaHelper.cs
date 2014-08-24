using System;
using System.IO;
using Sdl.Web.Mvc.Html;

namespace Sdl.Web.DD4T.Html
{
    /// <summary>
    /// Media helper implementation for DD4T
    /// </summary>
    public class DD4TMediaHelper : BaseMediaHelper
    {
        public DD4TMediaHelper()
        {
            ImageResizeUrlFormat = "{0}{1}{2}_n{3}";
        }

        /// <summary>
        /// Get a responsive image URL for a DD4T-rendered image
        /// </summary>
        /// <param name="url">Normal URL of the image</param>
        /// <param name="aspect">Aspect ratio to display</param>
        /// <param name="widthFactor">Width factor for the image (eg 100% or 250)</param>
        /// <param name="containerSize">Size (in grid units) of container element</param>
        /// <returns>A responsive image URL based on the passed parameters and client browser width and pixel ratio</returns>
        public override string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            string h = null;
            int width = GetResponsiveWidth(widthFactor, containerSize);
            //Round the width to the nearest set limit point - important as we do not want 
            //to swamp the cache with lots of different sized versions of the same image
            for (int i = 0; i < ImageWidths.Count; i++)
            {
                if (width <= ImageWidths[i])
                {
                    width = ImageWidths[i];
                    break;
                }
            }
            string w = String.Format("_w{0}", width);
            //Height is calculated from the aspect ratio (0 means preserve aspect ratio)
            if (aspect != 0)
            {
                h = String.Format("_h{0}", (int)Math.Ceiling(width / aspect));
            }
            //Build the URL
            string extension = Path.GetExtension(url);
            url = url.Substring(0, url.LastIndexOf(".", StringComparison.Ordinal));
            return String.Format(ImageResizeUrlFormat, url, w, h, extension);
        }
    }
}
