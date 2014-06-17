using Sdl.Web.Mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Common
{
    public interface IContentProvider
    {
        //Get specific page/entity content
        object GetPageModel(string url);
        string GetPageContent(string url);
        object GetEntityModel(string id);
        string GetEntityContent(string url);

        //Execute a query to get content
        void PopulateDynamicList(ContentList<Teaser> list);

        //Map the domain model to the presentation model
        object MapModel(object entity, ModelType modelType = ModelType.Entity, Type viewModeltype = null);
        
        //Process a url
        string ProcessUrl(string url);

        //Get view names from the domain model
        string GetEntityViewName(object entity);
        string GetRegionViewName(object region);
        string GetPageViewName(object page);
    }
}
