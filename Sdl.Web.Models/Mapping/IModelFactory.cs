using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public interface IModelFactory
    {
        object CreateEntityModel(object entity, Type viewModeltype = null);
        object CreatePageModel(object page, Dictionary<string, object> subPages = null, string view = null);
        string GetEntityViewName(object entity);
        string GetPageViewName(object entity);
        Type GetEntityViewModelType(object entity);
    }
}
