using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Modules.Search
{
    public interface ISearchProvider
    {
        SearchQuery<T> ExecuteQuery<T>(NameValueCollection parameters, SearchQuery<T> data);
    }
}
