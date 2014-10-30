using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    interface IModelProcessor
    {
        object ProcessModel(object sourceModel);
    }
}
