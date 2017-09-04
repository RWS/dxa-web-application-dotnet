using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.Rest
{
    public class TridionBinaryProvider : BaseProvider, IBinaryProvider
    {
        private const string controller = "binary";
        public TridionBinaryProvider(IProvidersCommonServices commonServices, IHttpMessageHandlerFactory httpClientFactory)
            :base(commonServices, httpClientFactory)
        {

        }
        //Temp fix: Remove after 01-01-2016; IHttpMessageHandlerFactory is registered in the DI. 
        //The DI needs to be upgraded for the registeration. below code prevent a runtime error in case that the DI is not upgraded.
        public TridionBinaryProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {

        }

        public byte[] GetBinaryByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetContentByUri/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<byte[]>(urlParameters);
        }

        public byte[] GetBinaryByUrl(string url)
        {
            var dotIndex = url.LastIndexOf('.');
            var extension = url.Substring(dotIndex + 1);
            var fileName = url.Substring(0, dotIndex);

            string urlParameters = string.Format("{0}/GetBinaryByUrl/{1}/{2}/{3}", controller, PublicationId, extension, fileName);
            //returns the content or empty string.
            return Execute<byte[]>(urlParameters);
        }

        [Obsolete("Retrieving binaries as a stream will be removed from the next version of DD4T")]
        public Stream GetBinaryStreamByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetBinaryStreamByUri/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<Stream>(urlParameters);
        }

        [Obsolete("Retrieving binaries as a stream will be removed from the next version of DD4T")]
        public Stream GetBinaryStreamByUrl(string url)
        {
            var dotIndex = url.LastIndexOf('.');
            var extension = url.Substring(dotIndex + 1);
            var fileName = url.Substring(0, dotIndex);

            string urlParameters = string.Format("{0}/GetBinaryStreamByUrl/{1}/{2}/{3}", controller, PublicationId, extension, fileName);
            //returns the content or empty string.
            return Execute<Stream>(urlParameters);
        }

        public DateTime GetLastPublishedDateByUrl(string url)
        {
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

        public string GetUrlForUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetUrlForUri/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<string>(urlParameters);
        }


        public IBinaryMeta GetBinaryMetaByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            string urlParameters = string.Format("{0}/GetBinaryMetaByUri/{1}/{2}", controller, tcmUri.PublicationId, tcmUri.ItemId);
            return Execute<BinaryMeta>(urlParameters);
        }

        public IBinaryMeta GetBinaryMetaByUrl(string url)
        {
            var dotIndex = url.LastIndexOf('.');
            var extension = url.Substring(dotIndex + 1);
            var fileName = url.Substring(0, dotIndex);

            string urlParameters = string.Format("{0}/GetBinaryMetaByUrl/{1}/{2}/{3}", controller, PublicationId, extension, fileName);
            //returns the content or empty string.
            return Execute<BinaryMeta>(urlParameters);
        }
    }
}
