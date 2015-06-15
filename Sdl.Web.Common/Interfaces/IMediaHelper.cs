
namespace Sdl.Web.Common.Interfaces
{
    public interface IMediaHelper
    {
        int GetResponsiveWidth(string widthFactor, int containerSize = 0);

        int GetResponsiveHeight(string widthFactor, double aspect, int containerSize = 0);

        string GetResponsiveImageUrl(string url, double aspect, string widthFactor, int containerSize = 0);

        bool ShowVideoPlaceholders
        {
            get; 
        }

        //The grid size used (bootstrap default @grid-columns = 12)
        int GridSize
        {
            get; 
        }

        //Screen size breakpoints 
        int LargeScreenBreakpoint
        {
            get; 
        }

        int MediumScreenBreakpoint
        {
            get; 
        }

        int SmallScreenBreakpoint
        {
            get; 
        }

        //Default media sizing
        double DefaultMediaAspect
        {
            get; 
        }

        string DefaultMediaFill
        {
            get; 
        }
    }
}
