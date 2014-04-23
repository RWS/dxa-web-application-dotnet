using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc
{
    public interface IStaticFileManager
    {
        void CreateStaticAssets(string applicationRoot);
        string Serialize(string url, string applicationRoot, string suffix, bool returnContents = false );
    }
}
