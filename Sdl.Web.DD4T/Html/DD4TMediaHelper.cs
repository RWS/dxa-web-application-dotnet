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
            ImageResizeUrlFormat = "{0}_w{1}_h{2}.{3}";
        }

        public override string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
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
            //Height is calculated from the aspect ratio
            int height = (int)Math.Ceiling(width / aspect);
            //Build the URL
            string extension = Path.GetExtension(url).Substring(1);
            url = url.Substring(0, url.LastIndexOf("."));
            return String.Format(ImageResizeUrlFormat, url, width, height, extension);
        }
    }
}
