using Sdl.Web.DD4T;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Site.Controllers
{
    public class SearchController : EntityController
    {
        public ActionResult Search(object entity)
        {
            //TODO - add search functionality
            return Entity(entity);
        }
    }
}
