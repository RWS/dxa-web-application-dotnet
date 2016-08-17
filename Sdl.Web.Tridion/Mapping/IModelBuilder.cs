using DD4T.ContentModel;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using IPage = DD4T.ContentModel.IPage;
using IComponentMeta = Tridion.ContentDelivery.Meta.IComponentMeta;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Interface for (DD4T-based) Model builders (advanced extension point).
    /// </summary>
    /// <remarks>
    /// Although an interface with a same name existed in STRI 1.0, this interface is not compatible.
    /// Preferably, this extension point should not be used in implementations, because it is DD4T specific and will change in DXA 2.0.
    /// It is kept in DXA 1.1 for advanced (SDL-owned) modules like the Smart Target module.
    /// </remarks>
    /// <seealso cref="ModelBuilderPipeline"/>
    public interface IModelBuilder
    {
        void BuildPageModel(ref PageModel pageModel, IPage page, IEnumerable<IPage> includes, Localization localization);
        void BuildEntityModel(ref EntityModel entityModel, IComponentPresentation cp, Localization localization);
        void BuildEntityModel(ref EntityModel entityModel, IComponent component, Type baseModelType, Localization localization);
        void BuildEntityModel(ref EntityModel entityModel, IComponentMeta componentMeta, Type baseModelType, Localization localization);
    }
}
