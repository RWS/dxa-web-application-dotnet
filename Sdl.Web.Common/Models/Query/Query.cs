using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    public abstract class Query
    {
        public Localization Localization { get; set; }
        public int MaxResults { get; set; }
        public int Start { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get { return (Start / PageSize) + 1; } }
    }
}
