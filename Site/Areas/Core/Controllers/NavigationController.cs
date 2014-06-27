using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Common;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Models;

namespace Site.Areas.Core.Controllers
{
    public class NavigationController : BaseController
    {      
        public NavigationController(IContentProvider contentProvider, IRenderer renderer)
        {
            ContentProvider = contentProvider;
            Renderer = renderer;
        }
    }
}
