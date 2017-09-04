using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class TridionPageProvider : BaseProvider, IPageProvider
    {
        private const string controller = "page";
        public TridionPageProvider(IProvidersCommonServices commonServices, IHttpMessageHandlerFactory httpClientFactory)
            :base(commonServices, httpClientFactory)
        {

        }
        //Temp fix: Remove after 01-01-2016; IHttpMessageHandlerFactory is registered in the DI. 
        //The DI needs to be upgraded for the registeration. below code prevent a runtime error in case that the DI is not upgraded.
        public TridionPageProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {

        }
        public string GetContentByUrl(string url)
        {
            var dotIndex = url.LastIndexOf('.');
            var extension = url.Substring(dotIndex + 1);
            var fileName = url.Substring(0, dotIndex);

            string urlParameters = string.Format("{0}/GetContentByUrl/{1}/{2}/{3}", controller, PublicationId, extension, fileName);
            //returns the content or empty string.
            return Execute<string>(urlParameters);
        }
            
        public string GetContentByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetContentByUri/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<string>(urlParameters);
        }

        public DateTime GetLastPublishedDateByUrl(string url)
        {
            //var a = url.Split('.');
            var dotIndex = url.LastIndexOf('.');
            var extension = url.Substring(dotIndex + 1);
            var fileName = url.Substring(0, dotIndex);

            string urlParameters = string.Format("{0}/GetLastPublishedDateByUrl/{1}/{2}/{3}", controller, PublicationId, extension, fileName);
            //returns the content or empty string.
            return Execute<DateTime>(urlParameters);
        }

        public DateTime GetLastPublishedDateByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetLastPublishedDateByUri/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<DateTime>(urlParameters);
        }

        public string[] GetAllPublishedPageUrls(string[] includeExtensions, string[] pathStarts)
        {
            throw new NotImplementedException();
        }

       
    }
}
