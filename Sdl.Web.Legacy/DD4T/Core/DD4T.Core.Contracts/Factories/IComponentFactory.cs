using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Querying;
using DD4T.ContentModel.Contracts.Caching;

namespace DD4T.ContentModel.Factories
{
    public interface IComponentFactory
    {
        [Obsolete]
        IComponentProvider ComponentProvider { get; set; }
        ICacheAgent CacheAgent { get; set; }
        bool TryGetComponent(string componentUri, out IComponent component, string templateUri = "");
        IComponent GetComponent(string componentUri, string templateUri = "");
        IList<IComponent> GetComponents(string[] componentUris);
		IList<IComponent> FindComponents(IQuery queryParameters);
        IList<IComponent> FindComponents(IQuery queryParameters, int pageIndex, int pageSize, out int totalCount);
        IComponent GetIComponentObject(string componentStringContent);
        DateTime GetLastPublishedDate(string uri);
    }
}
