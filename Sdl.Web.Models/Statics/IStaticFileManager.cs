using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc
{
    public interface IStaticFileManager
    {
        string SerializeForVersion(string url, string applicationRoot, string version, bool returnContents = false );
    }
}
