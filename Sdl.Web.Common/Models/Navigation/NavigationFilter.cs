namespace Sdl.Web.Common.Models.Navigation
{
    public class NavigationFilter
    {
        public bool IncludeAncestors { get; set; }
        public int DescendantLevels { get; set; }

        public NavigationFilter()
        {
            DescendantLevels = 1;
        }

        public override string ToString()
        {
            return string.Format(
                "IncludeAncestors={0}, DescendantLevels={1}", 
                IncludeAncestors, DescendantLevels
                );
        }
    }
}
