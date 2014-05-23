using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public interface IContentProvider
    {
        object GetPageModel(string url);
        string GetPageContent(string url);
        object GetEntityModel(string id);
        string GetEntityContent(string url);

        object MapModel(object entity, ModelType modelType = ModelType.Entity, Type viewModeltype = null, List<object> includes = null);
        
        string GetEntityViewName(object entity);
        string GetRegionViewName(object region);
        string GetPageViewName(object page);
    }
}
