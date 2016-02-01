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

        public bool ShowVideoPlaceholders { get; }
        public int GridSize { get; }
        public int LargeScreenBreakpoint { get; }
        public int MediumScreenBreakpoint { get; }
        public int SmallScreenBreakpoint { get; }
        public double DefaultMediaAspect { get; }
        public string DefaultMediaFill { get; }
    }
}
