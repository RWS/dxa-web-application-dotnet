
namespace Sdl.Web.Common.Interfaces
{
    public interface IMediaHelper
    {
        int GetResponsiveWidth(string widthFactor, int containerSize = 0);
        int GetResponsiveHeight(string widthFactor, double aspect, int containerSize = 0);
        string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0);
        bool ShowVideoPlaceholders { get; set; }
        //The grid size used (bootstrap default @grid-columns = 12)
        int GridSize { get; set; }
        //Screen size breakpoints 
        int LargeScreenBreakpoint { get; set; }
        int MediumScreenBreakpoint { get; set; }
        int SmallScreenBreakpoint { get; set; }
        //Default media sizing
        double DefaultMediaAspect { get; set; }
        string DefaultMediaFill { get; set; }

    }
}
