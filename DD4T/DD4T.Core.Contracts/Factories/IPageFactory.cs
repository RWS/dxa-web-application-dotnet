using System;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Resolvers;
namespace DD4T.ContentModel.Factories
{
    public interface IPageFactory
    {
        IPageProvider PageProvider { get; set; }
        ICacheAgent CacheAgent { get; set; }
        IPublicationResolver PublicationResolver { get; set; }
        bool TryFindPage(string url, out IPage page);
        IPage FindPage(string url);
        bool TryGetPage(string tcmUri, out IPage page);
        IPage GetPage(string tcmUri);
        bool TryFindPageContent(string url, out string pageContent);
        string FindPageContent(string url);
        bool TryGetPageContent(string tcmUri, out string pageContent);
        string GetPageContent(string tcmUri);
        bool HasPageChanged(string url);
        DateTime GetLastPublishedDateByUrl(string url);
		DateTime GetLastPublishedDateByUri(string uri);
        string[] GetAllPublishedPageUrls(string[] includeExtensions, string[] pathStarts);
        IPage GetIPageObject(string pageStringContent);
    }
}
