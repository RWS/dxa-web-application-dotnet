using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Mapping
{
    public interface IModelBuilder
    {
        object Create(object sourceEntity, Type type, List<object> includes);
    }
}
