using System;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.DD4T;

namespace Site.Controllers
{
    public class RegionController : DD4TController
    {
        public RegionController()
        {
            this.ContentProvider = new DD4TModelFactory();
            this.Renderer = new DD4TRenderer();
        }
    }
}
