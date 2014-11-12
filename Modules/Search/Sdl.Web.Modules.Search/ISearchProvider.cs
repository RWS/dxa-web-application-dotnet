using Sdl.Web.Common.Interfaces;
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
        IContentResolver ContentResolver { get; set; }
        SearchQuery<T> ExecuteQuery<T>(NameValueCollection parameters, SearchQuery<T> data, string searchIndex);
    }
}
