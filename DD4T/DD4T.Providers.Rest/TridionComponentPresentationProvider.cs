using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class TridionComponentPresentationProvider : BaseProvider, IComponentPresentationProvider
    {
        private const string controller = "componentpresentation";
        public TridionComponentPresentationProvider(IProvidersCommonServices commonServices, IHttpMessageHandlerFactory httpClientFactory)
            : base(commonServices, httpClientFactory)
        {

        }
        //Temp fix: Remove after 01-01-2016; IHttpMessageHandlerFactory is registered in the DI. 
        //The DI needs to be upgraded for the registeration. below code prevent a runtime error in case that the DI is not upgraded.
        public TridionComponentPresentationProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {

        }
        public string GetContent(string uri, string templateUri = "")
        {
            var tcmUri = new TcmUri(uri);
            string urlParameters = string.IsNullOrEmpty(templateUri) ?
                string.Format("{0}/GetContent/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId) :
                string.Format("{0}/GetContent/{1}/{2}/{3}", controller, tcmUri.PublicationId, tcmUri.ItemId, new TcmUri(templateUri).ItemId);

            return Execute<string>(urlParameters);
        }

        public DateTime GetLastPublishedDate(string uri)
        {
            TcmUri componentUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetLastPublishedDate/{1}/{2}", controller, componentUri.PublicationId, componentUri.ItemId);
            return Execute<DateTime>(urlParameters);
        }

        public List<string> GetContentMultiple(string[] componentUris)
        {
            throw new NotImplementedException();
        }

        public IList<string> FindComponents(ContentModel.Querying.IQuery queryParameters)
        {
            throw new NotImplementedException();
        }
    }
}
