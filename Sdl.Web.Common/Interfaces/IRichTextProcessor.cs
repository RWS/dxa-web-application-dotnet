using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Rich Text Processor extension point.
    /// </summary>
    public interface IRichTextProcessor
    {
        /// <summary>
        /// Processes rich text (XHTML) content.
        /// </summary>
        /// <param name="xhtml">The rich text content (XHTML fragment) to be processed.</param>
        /// <param name="localization">Context localization.</param>
        /// <returns>The processed rich text content as a mix of HTML fragments and Entity Models.</returns>
        /// <remarks>
        /// Typical rich text processing tasks: 
        /// <list type="bullet">
        ///     <item>Convert XHTML to plain HTML</item>
        ///     <item>Resolve inline links</item>
        /// </list>
        /// </remarks>
        RichText ProcessRichText(string xhtml, Localization localization);
    }
}
