using Sdl.Web.Mvc.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.DD4T.Html
{
    public class DD4TMediaHelper : BaseMediaHelper
    {
        public DD4TMediaHelper() : base()
        {
            ImageResizeUrlFormat = "{0}{1}{2}_n{3}";
        }

        public override string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            string w, h = null;
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
            w = String.Format("_w{0}",width);
            //Height is calculated from the aspect ratio (0 means preserve aspect ratio)
            if (aspect != 0)
            {
                h = String.Format("_h{0}",(int)Math.Ceiling(width / aspect));
            }
            //Build the URL
            string extension = Path.GetExtension(url);
            url = url.Substring(0, url.LastIndexOf("."));
            return String.Format(ImageResizeUrlFormat, url, w, h, extension);
        }
    }
}
