using System.Collections;
using System.Globalization;
using System.Resources;
using System.Web.Compilation;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// ASP.NET Resource Provider which obtains the resources from the current <see cref="Sdl.Web.Common.Configuration.Localization"/>.
    /// </summary>
    public class ResourceProvider : IResourceProvider
    {
        #region IResourceProvider members

        public object GetObject(string resourceKey, CultureInfo culture) 
            => WebRequestContext.Localization.GetResources(resourceKey)[resourceKey];

        public IResourceReader ResourceReader 
            => new ResourceReader(WebRequestContext.Localization.GetResources());

        #endregion
    }

    internal sealed class ResourceReader : IResourceReader
    {
        private readonly IDictionary _resources;

        public ResourceReader(IDictionary resources)
        {
            _resources = resources;
        }

        #region IResourceReader members

        public IDictionaryEnumerator GetEnumerator() => _resources.GetEnumerator();

        public void Close()
        {
            // Nothing to do
        }
        public void Dispose()
        {
            // Nothing to do
        }
        #endregion

        IEnumerator IEnumerable.GetEnumerator() => _resources.GetEnumerator();
    }
}
