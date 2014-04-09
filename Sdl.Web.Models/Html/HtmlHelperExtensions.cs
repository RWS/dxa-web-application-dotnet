using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Tridion.ContentDelivery.AmbientData;
using Sdl.Tridion.Context.Mvc;
using Sdl.Tridion.Context;

namespace Sdl.Web.Mvc.Html
{
    public static class HtmlHelperExtensions
    {
        public static IDictionary<Uri,object> AdfClaims(this HtmlHelper helper)
        {
            var claimStore = AmbientDataContext.CurrentClaimStore;
            return claimStore.GetAll();
        }

        public static string ResponsiveImageUrl(this HtmlHelper helper, string url, int baseSize, bool fixHeight = false)
        {
            //TODO calculate context things once, on request start?
            
            var context = new ContextEngine();
            double maxWidth = context.Device.PixelRatio * context.Browser.DisplayWidth * 0.84;
            int factor = (int)Math.Ceiling(maxWidth / baseSize);
            factor = factor > 4 ? 8 : factor==3 ? 4 : factor;//factor is 1x 2x 4x or 8x our base (small screen) width - as we only want to support 4 versions of an image, and want to cap it at base x 8
            factor = context.Device.PixelRatio == 1 && factor > 4 ? 4 : factor;//max x4 for pixel ratio of 1 
            int width = factor * baseSize;
            return String.Format("/cid/fit/{0}{1}/source/site{2}", fixHeight ? "x" : "", width, url);
        }

        public static string FixedSizeImageUrl(this HtmlHelper helper, string url, int size, bool fixHeight = true, double aspect = 1.62)
        {
            //default aspect is the Golden Ratio
            var context = new ContextEngine();
            size = size * context.Device.PixelRatio;
            int height = fixHeight ? size : (int) (size / aspect);
            int width = fixHeight ? (int)(size * aspect) : size;
            return String.Format("/cid/scale/{0}x{1}/source/site{2}", width, height, url);
        }

        public static ContextEngine Context(this HtmlHelper helper)
        {
            return new ContextEngine();
        }


    }
}
