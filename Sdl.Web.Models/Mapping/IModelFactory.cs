using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public interface IModelFactory
    {
        object CreateEntityModel(object entity,string view);
        object CreatePageModel(object page,string view = null,Dictionary<string,object> subPages = null);
    }
}
