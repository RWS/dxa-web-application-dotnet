using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Interfaces
{
    public interface IModelBuilder
    {
        ViewModel Create(object sourceEntity, Type type, List<PageModel> includes, MvcData mvcData = null);
    }
}
