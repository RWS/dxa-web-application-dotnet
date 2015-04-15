using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    public interface IContentResolver
    {
        string DefaultExtensionLessPageName { get; set; }
        string DefaultPageName { get; set; }
        string DefaultExtension { get; set; }
        string ResolveLink(object linkData, object resolveInstruction = null);
        object ResolveContent(object content, object resolveInstruction = null);
        MvcData ResolveMvcData(object data);
    }
}
