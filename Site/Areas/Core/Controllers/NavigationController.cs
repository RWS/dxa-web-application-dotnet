using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.Mvc;
using Sdl.Web.Common.Interfaces;
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
