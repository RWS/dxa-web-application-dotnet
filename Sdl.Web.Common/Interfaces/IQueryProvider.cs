using System.Collections.Generic;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    public interface IQueryProvider
    {
        bool HasMore { get; }
        IEnumerable<string> ExecuteQuery(SimpleBrokerQuery queryParams);
    }
}
