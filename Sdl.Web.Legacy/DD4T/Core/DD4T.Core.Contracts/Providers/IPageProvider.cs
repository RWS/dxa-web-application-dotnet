using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel.Contracts.Providers
{
    public interface IPageProvider : IProvider
    {
        string GetContentByUrl(string url);
        string GetContentByUri(string uri);
        DateTime GetLastPublishedDateByUrl(string url);
        DateTime GetLastPublishedDateByUri(string uri);
        string[] GetAllPublishedPageUrls(string[] includeExtensions, string[] pathStarts);
    }
}
