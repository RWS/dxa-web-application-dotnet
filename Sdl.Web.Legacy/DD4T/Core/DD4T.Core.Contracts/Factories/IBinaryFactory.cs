using System;
using DD4T.ContentModel.Contracts.Providers;
namespace DD4T.ContentModel.Factories
{
    public interface IBinaryFactory
    {
        IBinaryProvider BinaryProvider { get; set; }
        bool TryFindBinary(string url, out IBinary binary);
        IBinary FindBinary(string url);
        bool TryGetBinary(string tcmUri, out IBinary binary);       
        IBinary GetBinary(string tcmUri);
        bool TryFindBinaryContent(string url, out byte[] bytes);
        byte[] FindBinaryContent(string url);
        bool TryGetBinaryContent(string tcmUri, out byte[] bytes);
        byte[] GetBinaryContent(string tcmUri);
        bool HasBinaryChanged(string url);
        [Obsolete("Use FindBinaryMeta instead")]
        DateTime FindLastPublishedDate(string url);
        bool LoadBinariesAsStream { get; set; }
        string GetUrlForUri(string uri);
        bool FindAndStoreBinary(string url, string physicalPath);
        IBinaryMeta FindBinaryMeta(string url);
        IBinaryMeta GetBinaryMeta(string uri);

    }
}
