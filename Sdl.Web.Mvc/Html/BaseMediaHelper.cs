using System.IO;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;
using System;
using System.Collections.Generic;
using Sdl.Web.Mvc.Context;

namespace Sdl.Web.Mvc.Html
{  
    /// <summary>
    /// Media helpers are used to write out image/video URLs and to set responsive design features (screensize breakpoints etc.)
    /// </summary>
    public class BaseMediaHelper : IMediaHelper
    {
        public BaseMediaHelper()
        {
            //The Golden Ratio is our default aspect
            DefaultMediaAspect = 1.62;
            //The default fill for media is 100% of containing element
            DefaultMediaFill = "100%";

            //TODO publish from CMS to ensure is in sync with LESS variables etc.

            //Sensible defaults for image widths to optimize caching
            ImageWidths = new List<int> { 160, 320, 640, 1024, 2048 };
            GridSize = 12;
            LargeScreenBreakpoint = 1140;
            MediumScreenBreakpoint = 940;
            SmallScreenBreakpoint = 480;
            ShowVideoPlaceholders = true;

            ImageResizeUrlFormat = "{0}{1}{2}_n{3}";
        }     

        /// <summary>
        /// The grid size used (also set in LESS: @grid-columns: 12)
        /// </summary>
        public int GridSize
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Large screensize breakpoint (also set in LESS: @screen-lg: 1140px)
        /// </summary>
        public int LargeScreenBreakpoint
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Medium screensize breakpoint (also set in LESS: @screen-md: 940px)
        /// </summary>
        public int MediumScreenBreakpoint
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Small screensize breakpoint (also set in LESS: @screen-sm: 480px)
        /// </summary>
        public int SmallScreenBreakpoint
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Show placeholder images on videos before they are viewed
        /// </summary>
        public bool ShowVideoPlaceholders
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Default aspect ratio when rendering images/video (if none specified in the view code. Default is the golden ratio: 1.62)
        /// </summary>
        public double DefaultMediaAspect
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Default Media fill when rendering images/video (if none specified in the view code. Default is 100%)
        /// </summary>
        public string DefaultMediaFill
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Format string used for resized image URLs
        /// </summary>
        public string ImageResizeUrlFormat
        {
            get; 
            protected set;
        }

        /// <summary>
        /// A set of fixed widths for resized/responsive images - to optimize caching (default is 160, 320, 640, 1024, 2048)
        /// </summary>
        public static List<int> ImageWidths
        {
            get; 
            protected set;
        }
         
        /// <summary>
        /// Calulated the responsive media width based on client display size and pixel ration, grid container size and width factor. 
        /// </summary>
        /// <param name="widthFactor">The width factor</param>
        /// <param name="containerSize">The grid container size containing the media (in grid units)</param>
        /// <returns>The optimal media width</returns>
        public virtual int GetResponsiveWidth(string widthFactor, int containerSize = 0)
        {
            if (containerSize == 0)
            {
                //default is full width
                containerSize = GridSize;
            }
            if (string.IsNullOrEmpty(widthFactor))
            {
                widthFactor = DefaultMediaFill;
            }

            double width = 0;
            //For absolute fill factors, we should have a number
            if (!widthFactor.EndsWith("%"))
            {
                if (!double.TryParse(widthFactor, out width))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DefaultMediaFill);
                    //Change the fill factor to the default (100%)
                    widthFactor = DefaultMediaFill;
                }
                else
                {
                    width = width * WebRequestContext.ContextEngine.GetClaims<DeviceClaims>().PixelRatio;
                }
            }
            //For percentage fill factors, we need to do some calculation of container size etc.
            if (widthFactor.EndsWith("%"))
            {
                int fillFactor;
                if (!int.TryParse(widthFactor.Substring(0, widthFactor.Length - 1), out fillFactor))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DefaultMediaFill);
                }
                if (fillFactor == 0)
                {
                    fillFactor = int.Parse(DefaultMediaFill.Substring(0, DefaultMediaFill.Length - 1));
                }
                //TODO make the screen width behaviour configurable?
                switch (WebRequestContext.ScreenWidth)
                {
                    case ScreenWidth.ExtraSmall:
                        //Extra small screens are only one column
                        containerSize = SiteConfiguration.MediaHelper.GridSize;
                        break;
                    case ScreenWidth.Small:
                        //Small screens are max 2 columns
                        containerSize = (containerSize <= SiteConfiguration.MediaHelper.GridSize / 2 ? SiteConfiguration.MediaHelper.GridSize / 2 : SiteConfiguration.MediaHelper.GridSize);
                        break;
                }
                int cols = SiteConfiguration.MediaHelper.GridSize / containerSize;
                //TODO - should we make padding configurable?
                int padding = (cols - 1) * 30;
                //Get the max possible width
                width = WebRequestContext.MaxMediaWidth;
                //Factor the max possible width by the fill factor and container size and remove padding
                width = (fillFactor * containerSize * width / (SiteConfiguration.MediaHelper.GridSize * 100)) - padding;
            }
            return (int)Math.Ceiling(width);
        }

        /// <summary>
        /// Calulated the responsive media height based on client display size and pixel ration, grid container size, width factor and aspect ratio. 
        /// </summary>
        /// <param name="widthFactor">The width factor</param>
        /// <param name="aspect">The aspect ratio</param>
        /// <param name="containerSize">The grid container size containing the media (in grid units)</param>
        /// <returns>The optimal media height</returns>
        public virtual int GetResponsiveHeight(string widthFactor, double aspect, int containerSize = 0)
        {
            int width = GetResponsiveWidth(widthFactor, containerSize);
            return (int)Math.Ceiling(width / aspect);
        }

        /// <summary>
        /// Returns true if format defined by extension is supported.
        /// </summary>
        /// <param name="extension">File Extension of media file.</param>
        /// <returns></returns>
        public virtual bool IsSupported(string extension)
        {
            if (string.IsNullOrEmpty(extension)) return false;
            switch (extension.TrimStart('.').ToLower())
            {
                case "jpg":
                case "jpeg":
                    return true;
                case "gif":
                    return true;
                case "bmp":
                    return true;
                case "png":
                    return true;
            }
            return false;
        }
       
        /// <summary>
        /// Get a responsive image URL
        /// </summary>
        /// <param name="url">Normal URL of the image</param>
        /// <param name="aspect">Aspect ratio to display</param>
        /// <param name="widthFactor">Width factor for the image (eg 100% or 250)</param>
        /// <param name="containerSize">Size (in grid units) of container element</param>
        /// <returns>A responsive image URL based on the passed parameters and client browser width and pixel ratio</returns>
        public virtual string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            string extension = Path.GetExtension(url);
            if (!IsSupported(extension)) return url;
            string h = null;
            int width = GetResponsiveWidth(widthFactor, containerSize);
            //Round the width to the nearest set limit point - important as we do not want 
            //to swamp the cache with lots of different sized versions of the same image
            for (int i = 0; i < ImageWidths.Count; i++)
            {
                if (width <= ImageWidths[i] || i == ImageWidths.Count - 1)
                {
                    width = ImageWidths[i];
                    break;
                }
            }
            string w = $"_w{width}";
            //Height is calculated from the aspect ratio (0 means preserve aspect ratio)
            if (aspect != 0)
            {
                h = $"_h{(int) Math.Ceiling(width/aspect)}";
            }
            //Build the URL           
            url = url.Substring(0, url.LastIndexOf(".", StringComparison.Ordinal));
            return string.Format(ImageResizeUrlFormat, url, w, h, extension);
        }
    }
}
