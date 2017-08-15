using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Querying;
using DD4T.ContentModel.Contracts.Caching;

namespace DD4T.ContentModel.Factories
{
    public interface IComponentPresentationFactory
    {
        IComponentPresentationProvider ComponentPresentationProvider { get; set; }
        ICacheAgent CacheAgent { get; set; }
        IComponentPresentation GetComponentPresentation(string componentUri, string templateUri = "");
        bool TryGetComponentPresentation(out IComponentPresentation cp, string componentUri, string templateUri = "");
        IComponentPresentation GetIComponentPresentationObject(string componentPresentationStringContent);
        DateTime GetLastPublishedDate(string componentUri, string templateUri =  "");
        IList<IComponentPresentation> GetComponentPresentations(string[] componentUris); // TODO: think of a way to pass a list of 'component + template uris' (maybe Tuples?)
        IList<IComponentPresentation> FindComponentPresentations(IQuery queryParameters);
        IList<IComponentPresentation> FindComponentPresentations(IQuery queryParameters, int pageIndex, int pageSize, out int totalCount);
     }
}
