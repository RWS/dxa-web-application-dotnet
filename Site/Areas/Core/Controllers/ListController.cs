using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Common;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Tridion;

namespace Site.Areas.Core.Controllers
{
    public class ListController : EntityController
    {
        public ListController(IContentProvider contentProvider, IRenderer renderer) : base(contentProvider, renderer) { }
        
        [HandleSectionError(View = "_SectionError")]
        public ActionResult List(object entity, int containerSize = 0)
        {
            return Entity(entity, containerSize);
        }

        protected override object ProcessModel(object sourceModel, Type type)
        {
            var model = base.ProcessModel(sourceModel, type);
            var list = model as ContentList<Teaser>;
            if (list!=null)
            {
                if (list.ItemListElements.Count == 0)
                {
                    //we need to run a query to populate the list
                    int start = GetStart();
                    if (list.Id == Request.Params["id"])
                    {
                        //we only take the start from the query string if there is also an id parameter matching the model entity id
                        //this means that we are sure that the paging is coming from the right entity (if there is more than one paged list on the page)
                        list.CurrentPage = (start / list.PageSize) + 1;
                        list.Start = start;
                    }
                    this.ContentProvider.PopulateDynamicList(list);
                }
                model = list;
            }
            return model;
        }

        private int GetStart()
        {
            int res = 0;
            var start = Request.Params["start"];
            if (!String.IsNullOrEmpty(start))
            {
                Int32.TryParse(start, out res);
            }
            return res;
        }
    }
}
