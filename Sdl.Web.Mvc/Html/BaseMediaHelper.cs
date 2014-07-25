using System;
using System.Collections.Generic;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseMediaHelper : IMediaHelper
    {
        protected BaseMediaHelper()
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
        }

        //The grid size used (bootstrap default @grid-columns = 12)
        public int GridSize { get; set; }
        //Screen size breakpoints 
        public int LargeScreenBreakpoint { get; set; }
        public int MediumScreenBreakpoint { get; set; }
        public int SmallScreenBreakpoint { get; set; }
        public bool ShowVideoPlaceholders { get; set; }
        public double DefaultMediaAspect{ get; set; }
        public string DefaultMediaFill { get; set; }
        public string ImageResizeUrlFormat { get; set; }

        //A set of fixed widths for resized/responsive images - to optimize caching
        public static List<int> ImageWidths { get; set; }
         
        public virtual int GetResponsiveWidth(string widthFactor, int containerSize = 0)
        {
            if (containerSize == 0)
            {
                //default is full width
                containerSize = SiteConfiguration.MediaHelper.GridSize;
            }
            double width = 0;
            //For absolute fill factors, we should have a number
            if (!widthFactor.EndsWith("%"))
            {
                if (!Double.TryParse(widthFactor, out width))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DefaultMediaFill);
                    //Change the fill factor to the default (100%)
                    widthFactor = DefaultMediaFill;
                }
                else
                {
                    width = width * WebRequestContext.ContextEngine.Device.PixelRatio;
                }
            }
            //For percentage fill factors, we need to do some calculation of container size etc.
            if (widthFactor.EndsWith("%"))
            {
                int fillFactor;
                if (!Int32.TryParse(widthFactor.Substring(0, widthFactor.Length - 1), out fillFactor))
                {
                    Log.Warn("Invalid width factor (\"{0}\") when resizing image, defaulting to {1}", widthFactor, DefaultMediaFill);
                }
                if (fillFactor == 0)
                {
                    fillFactor = Int32.Parse(DefaultMediaFill.Substring(0, DefaultMediaFill.Length - 1));
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
                int padding = (cols - 1) * 20;
                //Get the max possible width
                width = WebRequestContext.MaxMediaWidth;
                //Factor the max possible width by the fill factor and container size and remove padding
                width = (fillFactor * containerSize * width / (SiteConfiguration.MediaHelper.GridSize * 100)) - padding;
            }
            return (int)Math.Ceiling(width);
        }

        public virtual int GetResponsiveHeight(string widthFactor, double aspect, int containerSize = 0)
        {
            int width = GetResponsiveWidth(widthFactor, containerSize);
            return (int)Math.Ceiling(width / aspect);
        }

        public abstract string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0);        
    }
}
