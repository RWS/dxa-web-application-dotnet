using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class TridionLinkProvider : BaseProvider, ILinkProvider
    {
        private const string controller = "link";
        public TridionLinkProvider(IProvidersCommonServices commonServices, IHttpMessageHandlerFactory httpClientFactory)
            :base(commonServices, httpClientFactory)
        {

        }
        //Temp fix: Remove after 01-01-2016; IHttpMessageHandlerFactory is registered in the DI. 
        //The DI needs to be upgraded for the registeration. below code prevent a runtime error in case that the DI is not upgraded.
        public TridionLinkProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {

        }
        public string ResolveLink(string componentUri)
        {
            TcmUri tcmUri = new TcmUri(componentUri);
            string urlParameters = string.Format("{0}/ResolveLink/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<string>(urlParameters);
        }

        public string ResolveLink(string sourcePageUri, string componentUri, string excludeComponentTemplateUri)
        {
            
            var compUri = new TcmUri(componentUri);
            var pageUri = new TcmUri(sourcePageUri);
            var templateUri = new TcmUri(excludeComponentTemplateUri);

            string urlParameters = string.Format("{0}/ResolveLink/{1}/{2}/{3}/{4}", controller, compUri.PublicationId, compUri.ItemId, pageUri.ItemId, templateUri.ItemId);
            return Execute<string>(urlParameters);
        }
    }
}
