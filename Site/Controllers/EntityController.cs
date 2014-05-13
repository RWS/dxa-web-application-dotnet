using System;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.DD4T;

namespace Site.Controllers
{
    public class EntityController : DD4TController
    {
        public EntityController()
        {
            this.ContentProvider = new DD4TModelFactory();
            this.Renderer = new DD4TRenderer();
        }
    }
}
