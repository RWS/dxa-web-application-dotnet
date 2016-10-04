using System;
using System.Collections.Generic;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Tridion.Tests
{
    internal class MockMediaHelper : IMediaHelper
    {
        private static readonly IList<ResponsiveImageRequest> _responsiveImageRequests = new List<ResponsiveImageRequest>(); 

        internal class ResponsiveImageRequest
        {
            internal string Url { get; set; }
            internal double Aspect { get; set; }
            internal string WidthFactor { get; set; }
            internal int ContainerSize { get; set;  }
            internal string ResponsiveImageUrl { get; set; }
        }

        public static IList<ResponsiveImageRequest> ResponsiveImageRequests
        {
            get { return _responsiveImageRequests; }
        } 

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
            string result = string.Format("{0}_a{1}_w{2}_cs{3}", url, aspect, widthFactor, containerSize);
            ResponsiveImageRequest responsiveImageRequest = new ResponsiveImageRequest
            {
                Url = url,
                Aspect = aspect,
                WidthFactor = widthFactor,
                ContainerSize = containerSize,
                ResponsiveImageUrl = result
            };
            _responsiveImageRequests.Add(responsiveImageRequest);

            return result;
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
