using Sdl.Web.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common
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
