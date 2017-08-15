using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel.Querying;

namespace DD4T.ContentModel.Contracts.Providers
{
    public interface IComponentPresentationProvider : IProvider
    {
        //string GetContent(string uri);
        string GetContent(string uri, string templateUri = "");
        DateTime GetLastPublishedDate(string uri);
        List<string> GetContentMultiple(string[] componentUris);
        IList<string> FindComponents(IQuery queryParameters);
    }

}
