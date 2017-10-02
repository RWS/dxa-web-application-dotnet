using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class BaseProvider : IProvider
    {
        private readonly IPublicationResolver _publicationResolver;
        protected readonly ILogger Logger;
        protected readonly IDD4TConfiguration Configuration;

        private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;

        public BaseProvider(IProvidersCommonServices commonServices, IHttpMessageHandlerFactory httpMessageHandlerFactory)
        {
            if (commonServices == null)
                throw new ArgumentNullException("commonServices");

            if (httpMessageHandlerFactory == null)
                throw new ArgumentNullException("httpMessageHandlerFactory");

            Logger = commonServices.Logger;
            _httpMessageHandlerFactory = httpMessageHandlerFactory;
            _publicationResolver = commonServices.PublicationResolver;
            Configuration = commonServices.Configuration;

        }

        //Temp fix: Remove after 01-01-2016; IHttpMessageHandlerFactory is registered in the DI. 
        //The DI needs to be upgraded for the registeration. below code prevent a runtime error in case that the DI is not upgraded.
        public BaseProvider(IProvidersCommonServices commonServices)
        {
            if (commonServices == null)
                throw new ArgumentNullException("commonServices");

            Logger = commonServices.Logger;
            _httpMessageHandlerFactory = new DefaultHttpMessageHandlerFactory();
            _publicationResolver = commonServices.PublicationResolver;
            Configuration = commonServices.Configuration;
        }

        private int publicationId = 0;
        public int PublicationId
        {
            get
            {
                if (publicationId == 0)
                    return _publicationResolver.ResolvePublicationId();

                return publicationId;
            }
            set
            {
                publicationId = value;
            }
        }

        public T Execute<T>(string urlParameters)
        {
            // add '/' at the end of url. needed to support '.' in the url the slash will be strip out in the 
            // DD4T.RestService.WebApi 

            if (!urlParameters.EndsWith("/"))
                urlParameters = string.Format("{0}/", urlParameters);

            HttpClientHandler messageHandler = new HttpClientHandler() { UseCookies = false };
            var pipeline = this._httpMessageHandlerFactory.CreatePipeline(messageHandler);

            using (var client = HttpClientFactory.Create(pipeline))
            {
                client.BaseAddress = new Uri(Configuration.ContentProviderEndPoint);
                // Add an Accept header for JSON format.
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                var message = new HttpRequestMessage(HttpMethod.Get, urlParameters);

                // read all http cookies and add it to the request. 
                // needed to enable session preview functionality
                try
                {
                    if(System.Web.HttpContext.Current != null)
                    {
                        var cookies = System.Web.HttpContext.Current.Request.Cookies;
                        var strBuilder = new StringBuilder();
                        foreach (var item in cookies.AllKeys)
                        {
                            var currentKey = string.Format("{0}={1};", item, cookies[item].Value);
                            Logger.Debug("RequestCookie found -> forwarding cookie -> {0}", currentKey);
                            strBuilder.Append(currentKey);
                        }
                        message.Headers.Add("Cookie", strBuilder.ToString());
                    }
                }
                catch
                {
                    Logger.Warning("HttpContext is not initialized yet..");
                }
                
                var result = client.SendAsync(message).Result;
                if(result.IsSuccessStatusCode)
                {
                    return result.Content.ReadAsAsync<T>().Result;
                }
            }
            return default(T);
        }

    }
}
