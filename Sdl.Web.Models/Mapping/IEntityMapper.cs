using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    public interface IEntityMapper
    {
        object GetPropertyValue(object sourceEntity, List<SemanticFieldProperty> properties);
    }
}
