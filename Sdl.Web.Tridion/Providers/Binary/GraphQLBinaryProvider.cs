using System;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.GraphQLClient;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.Tridion.PCAClient;
using Sdl.Web.HttpClient.Request;

namespace Sdl.Web.Tridion.Providers.Binary
{
    public class GraphQLBinaryProvider : IBinaryProvider
    {
        public DateTime GetBinaryLastPublishedDate(ILocalization localization, string urlPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(ContentNamespace.Sites, int.Parse(localization.Id), urlPath, null);
            return DateTime.ParseExact(binary.InitialPublishDate, "MM/dd/yyyy HH:mm:ss", null);
        }

        public DateTime GetBinaryLastPublishedDate(ILocalization localization, int binaryId)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(ContentNamespace.Sites, int.Parse(localization.Id), binaryId, null);
            return DateTime.ParseExact(binary.InitialPublishDate, "MM/dd/yyyy HH:mm:ss", null);
        }

        public byte[] GetBinary(ILocalization localization, int binaryId, out string binaryPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(ContentNamespace.Sites, int.Parse(localization.Id), binaryId, null);
            return GetBinaryData(client, binary, out binaryPath);
        }

        public byte[] GetBinary(ILocalization localization, string urlPath, out string binaryPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(ContentNamespace.Sites, int.Parse(localization.Id), urlPath, null);
            return GetBinaryData(client, binary, out binaryPath);
        }

        protected virtual byte[] GetBinaryData(IGraphQLClient client, BinaryComponent binaryComponent, out string binaryPath)
        {
            binaryPath = null;
            if (binaryComponent?.Variants == null) return null;
            var variant = binaryComponent.Variants.Edges[0].Node;
            binaryPath = variant.Path;
            return client.HttpClient.Execute<byte[]>(new HttpClientRequest
            {
                AbsoluteUri = variant.DownloadUrl.Replace("dxadevweb85.ams.dev", "localhost")
            }).ResponseData;
        }
    }
}
