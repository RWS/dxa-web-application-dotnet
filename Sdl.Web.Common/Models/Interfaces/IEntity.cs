using System.Collections.Generic;

namespace Sdl.Web.Common.Models
{
    public interface IEntity
    {
        Dictionary<string, string> EntityData{get;set;}
        Dictionary<string, string> PropertyData{get;set;}
        string Id{get;set;}
    }
}
