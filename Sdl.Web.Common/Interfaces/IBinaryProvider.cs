using System;
using System.Threading;
using System.Threading.Tasks;
using Sdl.Web.Common.Configuration;

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
        DateTime GetBinaryLastPublishedDate(Localization localization, string urlPath);

        /// <summary>
        /// Get the last published date of the binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="urlPath">Binary Url</param>
        /// <returns>Last Publish Date</returns>
        Task<DateTime> GetBinaryLastPublishedDateAsync(Localization localization, string urlPath, CancellationToken cancellationToken);

        /// <summary>
        /// Get the last published date of the binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="binaryId">Binary Id</param>
        /// <returns>Last Published Date</returns>
        DateTime GetBinaryLastPublishedDate(Localization localization, int binaryId);

        /// <summary>
        /// Get the last published date of the binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="binaryId">Binary Id</param>
        /// <returns>Last Published Date</returns>
        Task<DateTime> GetBinaryLastPublishedDateAsync(Localization localization, int binaryId, CancellationToken cancellationToken);

        /// <summary>
        /// Get Binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="urlPath">Binary Url</param>
        /// <param name="binaryPath">Path to binary</param>
        /// <returns>Tuple containing Binary Data and the path to the binary</returns>
        Tuple<byte[],string> GetBinary(Localization localization, string urlPath);

        /// <summary>
        /// Get Binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="binaryId">Binary Id</param>
        /// <param name="binaryPath">Path to binary</param>
        /// <returns>Tuple containing Binary Data and the path to the binary</returns>
        Tuple<byte[],string> GetBinary(Localization localization, int binaryId);

        /// <summary>
        /// Get Binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="urlPath">Binary Url</param>
        /// <param name="binaryPath">Path to binary</param>
        /// <returns>Tuple containing Binary Data and the path to the binary</returns>
        Task<Tuple<byte[],string>> GetBinaryAsync(Localization localization, string urlPath, CancellationToken cancellationToken);

        /// <summary>
        /// Get Binary
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <param name="urlPath">Binary Url</param>
        /// <param name="binaryPath">Path to binary</param>
        /// <returns>Tuple containing Binary Data and the path to the binary</returns>
        Task<Tuple<byte[],string>> GetBinaryAsync(Localization localization, int binaryId, CancellationToken cancellationToken);
    }
}
