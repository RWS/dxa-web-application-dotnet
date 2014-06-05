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
        public static List<int> ImageWidths{get;set;}
        //The grid size used (bootstrap default @grid-columns = 12)
        public static int GridSize{get;set;}
        //The image resizing URL
        public const string ImageResizeUrl = "/{0}/scale/{1}x{2}/source/site{3}";
        //The image resizing route
        public static string ImageResizeRoute {get;set;}
        //Screen size breakpoints 
        public static int LargeScreenBreakpoint{get;set;}
        public static int MediumScreenBreakpoint{get;set;}
        public static int SmallScreenBreakpoint { get; set; }
    }
}
