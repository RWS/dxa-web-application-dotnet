using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Context
{
    public static class ContextConfiguration
    {
        //The grid size used (bootstrap default @grid-columns = 12)
        public static int GridSize{get;set;}
        //Screen size breakpoints 
        public static int LargeScreenBreakpoint{get;set;}
        public static int MediumScreenBreakpoint{get;set;}
        public static int SmallScreenBreakpoint { get; set; }
    }
}
