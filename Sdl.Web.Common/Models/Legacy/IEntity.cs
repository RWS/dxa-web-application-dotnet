using System;
using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    [Obsolete("Deprecated in DXA 1.1. Use class EntityModel instead.")]
    public interface IEntity
    {
        Dictionary<string, string> EntityData{get;set;}
        Dictionary<string, string> PropertyData{get;set;}
        string Id { get; set; }
        MvcData AppData { get; set; }
    }
}
