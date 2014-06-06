using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Tridion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Site.Controllers
{
    public class ListController : EntityController
    {
        [HandleSectionError(View = "_SectionError")]
        public ActionResult List(object entity, int containerSize = 0)
        {
            return Entity(entity, containerSize);
        }

        protected override object ProcessModel(object sourceModel, Type type, List<string> includes = null)
        {
            var model = base.ProcessModel(sourceModel, type, includes);
            var list = model as ContentList<Teaser>;
            if (list!=null)
            {
                if (list.ItemListElements.Count == 0)
                {
                    //we need to run a query to populate the list
                    int start = GetStart();
                    ContentQuery query = new ContentQuery();
                    if (list.Id == Request.Params["id"])
                    {
                        //we only take the start from the query string if there is also an id parameter matching the model entity id
                        //this means that we are sure that the paging is coming from the right entity (if there is more than one paged list on the page)
                        query.Start = start;
                        list.CurrentPage = (start / list.PageSize) + 1;
                        list.Start = start;
                    }
                    //todo - we introduce a dependency on Tridion here - perhaps get the query object from the ContentProvider?
                    query.PublicationId = WebRequestContext.Localization.LocalizationId;
                    query.PageSize = list.PageSize;
                    query.ContentProvider = this.ContentProvider;
                    query.SchemaId = MapSchema(list.ContentType.Key);
                    list.ItemListElements = query.ExecuteQuery();
                    list.HasMore = query.HasMore;
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

        private int MapSchema(string schemaName)
        {
            //TODO - what if the schema is from a different module?
            int res = 0;
            var schemaId = Configuration.GetGlobalConfig("schemas." + schemaName.Substring(0,1).ToLower() + schemaName.Substring(1));
            Int32.TryParse(schemaId, out res);
            return res;
        }
    }
}
