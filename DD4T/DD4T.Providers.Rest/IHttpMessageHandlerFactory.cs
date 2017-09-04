using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public interface IHttpMessageHandlerFactory
    {
        HttpMessageHandler CreatePipeline(HttpMessageHandler innerhandler);
    }
}
