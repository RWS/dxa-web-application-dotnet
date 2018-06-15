using System.Collections;

namespace Sdl.Web.Common.Interfaces
{
    public interface ILocalizationResources
    {
        /// <summary>
        /// Force reload of resources.
        /// </summary>
        void Reload();

        /// <summary>
        /// Gets a configuration value with a given key.
        /// </summary>
        /// <param name="key">The configuration key, in the format section.name.</param>
        /// <returns>The configuration value.</returns>
        string GetConfigValue(string key);

        /// <summary>
        /// Gets resources.
        /// </summary>
        /// <param name="sectionName">Optional name of the section for which to get resource. If not specified (or <c>null</c>), all resources are obtained.</param>
        IDictionary GetResources(string sectionName = null);
    }
}
