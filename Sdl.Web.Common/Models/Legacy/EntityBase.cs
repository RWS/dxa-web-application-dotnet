using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Legacy base class for View Models for Entities.
    /// </summary>
    [Obsolete("Deprecated in DXA 1.1. Use class EntityModel instead.")]
#pragma warning disable 618
    public class EntityBase : EntityModel, IEntity
#pragma warning restore 618
    {
        [SemanticProperty(IgnoreMapping = true)]
        [Obsolete("Deprecated in DXA 1.1. Use property XpmMetadata instead.")]
        public Dictionary<string, string> EntityData
        {
            get
            {
                return XpmMetadata as Dictionary<string, string>;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property XpmMetadata instead.");
            }
        }

        [SemanticProperty(IgnoreMapping = true)]
        [Obsolete("Deprecated in DXA 1.1. Use property XpmPropertyMetadata instead.")]
        public Dictionary<string, string> PropertyData
        {
            get
            {
                return XpmPropertyMetadata as Dictionary<string, string>;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property XpmPropertyMetadata instead.");
            }
        }

        [SemanticProperty(IgnoreMapping = true)]
        [Obsolete("Deprecated in DXA 1.1. Use property MvcData instead.")]
        public MvcData AppData
        {
            get
            {
                return MvcData;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property MvcData instead.");
            }
        }
    }
}