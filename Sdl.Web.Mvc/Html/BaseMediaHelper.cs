using Sdl.Web.Mvc.Common;
using Sdl.Web.Mvc.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Html
{
    public abstract class BaseMediaHelper : IMediaHelper
    {
        public BaseMediaHelper()
        {
            //The Golden Ratio is our default aspect
            DefaultMediaAspect = 1.62;
            //The default fill for media is 100% of containing element
            DefaultMediaFill = "100%";
            //Sensible defaults for image widths
            ImageWidths = new List<int> { 160, 320, 640, 1024, 2048 };
        }
        
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
                containerSize = ContextConfiguration.GridSize;
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
                        containerSize = ContextConfiguration.GridSize;
                        break;
                    case ScreenWidth.Small:
                        //Small screens are max 2 columns
                        containerSize = (containerSize <= ContextConfiguration.GridSize / 2 ? ContextConfiguration.GridSize / 2 : ContextConfiguration.GridSize);
                        break;
                }
                int cols = ContextConfiguration.GridSize / containerSize;
                //TODO - should we make padding configurable?
                int padding = (cols - 1) * 20;
                //Get the max possible width
                width = WebRequestContext.MaxMediaWidth;
                //Factor the max possible width by the fill factor and container size and remove padding
                width = (fillFactor * containerSize * width / (ContextConfiguration.GridSize * 100)) - padding;
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
