using System;

namespace Sdl.Web.Common.Interfaces
{   
    /// <summary>
    /// Data Model Extensions
    /// </summary>
    public interface IDataModelExtension
    {
        /// <summary>
        /// Returns type given a type name and assembly name
        /// <remarks>
        /// This allows mapping of type names to actual types for the data model deserialization.
        /// You can perform type mapping also by implementing this interface in your model builder pipeline.
        /// </remarks>
        /// </summary>
        /// <param name="assemblyName">Assembly name (may be null)</param>
        /// <param name="typeName">Type name</param>
        /// <returns></returns>
        Type ResolveDataModelType(string assemblyName, string typeName);
    }
}