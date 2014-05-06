using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using Sdl.Web.DD4T;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.Mvc.Models;

namespace Site.Controllers
{
    public class NavigationController : DD4TController
    {
        private SitemapItem navigationModel;
        public NavigationController()
        {
            this.ModelFactory = new DD4TModelFactory();
            this.Renderer = new DD4TRenderer();            
        }






    }
}
