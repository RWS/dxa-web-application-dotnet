using System;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests
{
    internal class TestMediaHelper : IMediaHelper
    {
        public int GetResponsiveWidth(string widthFactor, int containerSize = 0)
        {
            throw new NotImplementedException();
        }

        public int GetResponsiveHeight(string widthFactor, double aspect, int containerSize = 0)
        {
            throw new NotImplementedException();
        }

        public string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0)
        {
            throw new NotImplementedException();
        }

        public bool ShowVideoPlaceholders { get; set; }
        public int GridSize { get; set; }
        public int LargeScreenBreakpoint { get; set; }
        public int MediumScreenBreakpoint { get; set; }
        public int SmallScreenBreakpoint { get; set; }
        public double DefaultMediaAspect { get; set; }
        public string DefaultMediaFill { get; set; }
    }
}
