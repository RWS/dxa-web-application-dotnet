using System.Collections.Generic;

namespace Sdl.Web.Common.Interfaces
{
    /// <summary>
    /// Interface for the Context Claims Provider extension point
    /// </summary>
    public interface IContextClaimsProvider
    {
        /// <summary>
        /// Gets the context claims. Either all context claims or for a given aspect name.
        /// </summary>
        /// <param name="aspectName">The aspect name. If <c>null</c> all context claims are returned.</param>
        /// <param name="localization">The context Localization.</param>
        /// <returns>A dictionary with the claim names in format aspectName.propertyName as keys.</returns>
        IDictionary<string, object> GetContextClaims(string aspectName, ILocalization localization);

        /// <summary>
        /// Gets the device family (an aggregated device claim determined from other context claims).
        /// </summary>
        /// <returns>A string representing the device family.</returns>
        string GetDeviceFamily();
    }
}
