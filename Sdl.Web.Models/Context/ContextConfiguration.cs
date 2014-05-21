using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Context
{
    public static class ContextConfiguration
    {
        //A set of fixed widths for resized/responsive images - to optimize caching
        private static List<int> _imageWidths = new List<int> {160,320,640,1024,2048};
        //The grid size used (bootstrap default @grid-columns = 12)
        private static int _gridSize = 12;
        //The image resizing URL
        public const string ImageResizeUrl = "/{0}/scale/{1}x{2}/source/site{3}";
        //The image resizing route
        public const string ImageResizeRoute = "cid";

        public static List<int> ImageWidths
        {
            get
            {
                return _imageWidths;
            }
            set
            {
                _imageWidths = value;
            }
        }
        public static int GridSize
        {
            get
            {
                return _gridSize;
            }
            set
            {
                _gridSize = value;
            }
        }
    }
}
