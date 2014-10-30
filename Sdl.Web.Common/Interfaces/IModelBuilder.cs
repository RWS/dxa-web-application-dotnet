using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Interfaces
{
    public interface IModelBuilder
    {
        object Create(object sourceEntity, Type type, List<object> includes, MvcData mvcData = null);
    }
}
