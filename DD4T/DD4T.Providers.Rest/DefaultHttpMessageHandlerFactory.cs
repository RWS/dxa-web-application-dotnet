using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class DefaultHttpMessageHandlerFactory : IHttpMessageHandlerFactory
    {
        public HttpMessageHandler CreatePipeline(HttpMessageHandler innerhandler)
        {
            var emptyList = new List<DelegatingHandler>();
            return HttpClientFactory.CreatePipeline(innerhandler, emptyList.ToArray());
        }
    }
}
