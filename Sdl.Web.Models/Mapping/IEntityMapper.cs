using System.Collections.Generic;

namespace Sdl.Web.Mvc.Mapping
{
    /// <summary>
    /// Entity Mapper interface.
    /// </summary>
    public interface IEntityMapper
    {
        /// <summary>
        /// Get value based on semantic property
        /// </summary>
        /// <param name="sourceEntity"></param>
        /// <param name="semantics"></param>
        /// <returns></returns>
        object GetPropertyValue(object sourceEntity, List<FieldSemantics> semantics);
    }
}
