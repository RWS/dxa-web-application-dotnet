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
                    //todo - we introduce a dependency on Tridion here - perhaps get the query object from the ContentProvider?
                    ContentQuery query = new ContentQuery();
                    query.PublicationId = WebRequestContext.Localization.LocalizationId;
                    query.PageSize = list.PageSize;
                    query.Start = list.Start;
                    query.ContentProvider = this.ContentProvider;
                    query.SchemaId = MapSchema(list.ContentType);
                    list.ItemListElements = query.ExecuteQuery();
                }
                model = list;
            }
            return model;
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
