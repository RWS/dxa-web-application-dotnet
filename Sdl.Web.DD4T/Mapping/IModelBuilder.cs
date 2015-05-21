using DD4T.ContentModel;
using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using IPage = DD4T.ContentModel.IPage;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// Interface for (DD4T-based) Model builders (advanced extension point).
    /// </summary>
    /// <remarks>
    /// Although an interface with a same name existed in STRI 1.0, this interface is not compatible (it is more strongly typed).
    /// Preferably, this extension point should not be used in implementations, because it is DD4T specific and will disappear in DXA 2.0.
    /// It is kept in DXA 1.1 as an emergency measure in case the default DD4TModelBuilder is insufficient.
    /// </remarks>
    public interface IModelBuilder
    {
        PageModel CreatePageModel(IPage page, Type type, List<PageModel> includes, MvcData mvcData);
        EntityModel CreateEntityModel(IComponentPresentation cp, Type type, MvcData mvcData);
        EntityModel CreateEntityModel(IComponent component, Type type);
    }
}
