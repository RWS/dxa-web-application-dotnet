using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Models
{
    public abstract class Query
    {
        public ILocalization Localization { get; set; }
        public int MaxResults { get; set; }
        public int Start { get; set; }
        public string Cursor { get; set; }
        public int PageSize { get; set; }
    }
}
