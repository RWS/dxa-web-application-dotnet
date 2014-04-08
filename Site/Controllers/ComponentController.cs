using System;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.DD4T;

namespace Site.Controllers
{
    public class ComponentController : DD4TController
    {
        public ComponentController()
        {
            this.ModelFactory = new DD4TModelFactory();
            this.Renderer = new DD4TRenderer();
        }
    }
}
