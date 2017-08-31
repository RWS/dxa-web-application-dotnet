using System;
using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.ModelService
{
    public class UriCreator
    {
        private readonly Dictionary<string, object> _queryParams = new Dictionary<string, object>();
        private readonly Uri _baseUri;
        private string _path;

        protected UriCreator(Uri baseUri)
        {
            _baseUri = baseUri;
        }

        public static UriCreator FromString(string baseUri) => new UriCreator(new Uri(baseUri));

        public static UriCreator FromUri(Uri baseUri) => new UriCreator(baseUri);

        public UriCreator WithPath(string path)
        {
            if (_baseUri.ToString().EndsWith("/")) path = path.TrimStart('/');
            _path = path;
            return this;
        }

        public UriCreator WithQueryParam(string key, object value)
        {
            _queryParams.Add(key, value);
            return this;
        }

        public Uri Build()
            =>
                _queryParams.Count > 0
                    ? new Uri(_baseUri, _path + "?" + string.Join("&", _queryParams.Select(x => x.Key + "=" + x.Value)))
                    : new Uri(_baseUri, _path);
    }
}
