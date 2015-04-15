using System;
using Sdl.Web.Common.Models;

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
        
        //Execute a query to get content
        void PopulateDynamicList(ContentList<Teaser> list);        
    }
}