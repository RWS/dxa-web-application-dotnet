using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Modules.Search
{
    public interface ISearchProvider
    {
        SearchResults<Teaser> ExecuteQuery(SearchResults<Teaser> data);
    }
}
