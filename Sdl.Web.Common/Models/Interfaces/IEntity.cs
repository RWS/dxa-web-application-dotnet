using Sdl.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Models.Interfaces
{
    public interface IEntity
    {
        Dictionary<string, string> EntityData{get;set;}
        Dictionary<string, string> PropertyData{get;set;}
        string Id{get;set;}
    }
}
