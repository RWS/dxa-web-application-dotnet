using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using DD4T.ContentModel;

namespace Sdl.Web.DD4T
{
    public interface IBinaryFileManager
    {
        bool ProcessRequest(HttpRequest request);
    }
}
