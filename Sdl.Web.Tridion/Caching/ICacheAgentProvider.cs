using DD4T.ContentModel.Contracts.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Tridion.Caching
{
    /// <summary>
    /// DD4T Cache Agent provider interface
    /// </summary>
    public interface ICacheAgentProvider
    {
        ICacheAgent CacheAgent { get; }
    }
}
