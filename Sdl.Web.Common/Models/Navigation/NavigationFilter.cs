namespace Sdl.Web.Common.Models.Navigation
{
    public class NavigationFilter
    {
        public bool IncludeAncestors { get; set; }
        public int DescendantLevels { get; set; }
        public bool IncludeRelated { get; set; }
        public bool IncludeCustomMetadata { get; set; }

        public NavigationFilter()
        {
            DescendantLevels = 1;
        }

        public override string ToString()
        {
            return string.Format(
                "IncludeAncestors={0], DecendantLevels={1}, IncludeRelated={2} IncludeCustomMetadata={3}", 
                IncludeAncestors, DescendantLevels, IncludeRelated, IncludeCustomMetadata
                );
        }
    }
}
