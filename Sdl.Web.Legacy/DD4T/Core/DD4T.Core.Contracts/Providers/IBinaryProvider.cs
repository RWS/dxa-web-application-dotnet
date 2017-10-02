using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ContentModel.Factories;
using System.IO;

namespace DD4T.ContentModel.Contracts.Providers
{
    public interface IBinaryProvider : IProvider
    {
        byte[] GetBinaryByUri(string uri);
        byte[] GetBinaryByUrl(string url);
        Stream GetBinaryStreamByUri(string uri);
        Stream GetBinaryStreamByUrl(string url);
        [Obsolete("Use GetBinaryMetaByUri instead")]
        DateTime GetLastPublishedDateByUri(string uri);
        [Obsolete("Use GetBinaryMetaByUrl instead")]
        DateTime GetLastPublishedDateByUrl(string url);
        string GetUrlForUri(string uri);

        IBinaryMeta GetBinaryMetaByUri(string uri);
        IBinaryMeta GetBinaryMetaByUrl(string url);

    }
}
