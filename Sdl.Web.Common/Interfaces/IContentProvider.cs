using Sdl.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    public interface IContentProvider
    {
        IContentResolver ContentResolver { get; set; }

        //Get specific page/entity content
        object GetPageModel(string url);
        string GetPageContent(string url);
        object GetEntityModel(string id);
        string GetEntityContent(string url);
        object GetNavigationModel(string url);

        //Map the domain model to the presentation model
        object MapModel(object entity, ModelType modelType = ModelType.Entity, Type viewModeltype = null);
        
        //Get view data from the domain model
        ViewData GetEntityViewData(object entity);
        ViewData GetRegionViewData(object region);
        ViewData GetPageViewData(object page);

        //Execute a query to get content
        void PopulateDynamicList(ContentList<Teaser> list);
        
    }
}
