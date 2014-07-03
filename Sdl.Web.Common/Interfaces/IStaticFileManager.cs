using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    public interface IStaticFileManager
    {
        string CreateStaticAssets(string applicationRoot);
        string Serialize(string url, bool returnContents = false );
    }
}
