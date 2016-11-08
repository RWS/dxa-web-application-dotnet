using System;

namespace Sdl.Web.Common
{
    /// <summary>
    /// Exception thrown by DXA Content Providers if an item is not found.
    /// </summary>
    [Serializable]
    public class DxaUnknownLocalizationException : DxaException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DxaUnknownLocalizationException"/>.
        /// </summary>
        /// <param name="url">The URL which couldn't be mapped to a Localization.</param>
        public DxaUnknownLocalizationException(string message)
            : base(message)
        {
        }
    }
}
