using System;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    [Obsolete("Deprecated in DXA 1.1. Use ILinkResolver and/or IRichTextProcessor instead. Custom Content Resolvers have to be rewritten using these new extension points.")]
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
