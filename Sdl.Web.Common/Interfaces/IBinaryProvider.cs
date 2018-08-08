using System;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for Binary Provider extension point.
    /// </summary>
    public interface IBinaryProvider
    {
        /// <summary>
        /// Get the last published date of the binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="urlPath">Binary Url</param>
        /// <returns>Last Publish Date</returns>
        DateTime GetBinaryLastPublishedDate(ILocalization localization, string urlPath);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="binaryId">Binary Id</param>
        /// <returns>Last Published Date</returns>
        DateTime GetBinaryLastPublishedDate(ILocalization localization, int binaryId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="urlPath">Binary Url</param>
        /// <param name="binaryPath">Path to binary</param>
        /// <returns>Binary Data</returns>
        byte[] GetBinary(ILocalization localization, string urlPath, out string binaryPath);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="binaryId">Binary Id</param>
        /// <param name="binaryPath">Path to binary</param>
        /// <returns>Binary Data</returns>
        byte[] GetBinary(ILocalization localization, int binaryId, out string binaryPath);
    }
}
