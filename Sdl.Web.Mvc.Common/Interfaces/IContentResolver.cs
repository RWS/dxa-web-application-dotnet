using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Interfaces
{
    public interface IContentResolver
    {
        string DefaultExtensionLessPageName { get; set; }
        string DefaultPageName { get; set; }
        string DefaultExtension { get; set; }
        string ResolveLink(object linkData, object resolveInstruction = null);
        object ResolveContent(object content, object resolveInstruction = null);
    }
}
