using System;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.GraphQLClient;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.Tridion.PCAClient;
using Sdl.Web.HttpClient.Request;
using Sdl.Web.PublicContentApi.Utils;

namespace Sdl.Web.Tridion.Providers.Binary
{
    /// <summary>
    /// Binary Provider
    /// </summary>
    public class GraphQLBinaryProvider : IBinaryProvider
    {
        protected static readonly string DateTimeFormat = "MM/dd/yyyy HH:mm:ss";

        protected ContentNamespace GetNamespace(ILocalization localization)
           => CmUri.NamespaceIdentiferToId(localization.CmUriScheme);

        public DateTime GetBinaryLastPublishedDate(ILocalization localization, string urlPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), urlPath, null);
            return binary == null ? DateTime.MinValue : DateTime.ParseExact(binary.InitialPublishDate, DateTimeFormat, null);
        }

        public DateTime GetBinaryLastPublishedDate(ILocalization localization, int binaryId)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), binaryId, null);
            return binary == null ? DateTime.MinValue : DateTime.ParseExact(binary.InitialPublishDate, DateTimeFormat, null);
        }

        public byte[] GetBinary(ILocalization localization, int binaryId, out string binaryPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), binaryId, null);
            var data = GetBinaryData(client, binary, out binaryPath);
            if (data == null) throw new DxaItemNotFoundException(binaryId.ToString(), localization.Id);
            return data;
        }

        public byte[] GetBinary(ILocalization localization, string urlPath, out string binaryPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), urlPath, null);
            var data = GetBinaryData(client, binary, out binaryPath);
            if(data == null) throw new DxaItemNotFoundException(urlPath, localization.Id);
            return data;
        }

        protected virtual byte[] GetBinaryData(IGraphQLClient client, BinaryComponent binaryComponent, out string binaryPath)
        {
            binaryPath = null;
            if (binaryComponent == null) return null;
          
            if (binaryComponent?.Variants == null)
            {
                Log.Error("Unable to get binary data for CmUri: " + binaryComponent.CmUri());
                return null;
            }
            var variant = binaryComponent.Variants.Edges[0].Node;
            binaryPath = variant.Path;
            try
            {
                Log.Debug("Attempting to get binary at : " + variant.DownloadUrl);
                return client.HttpClient.Execute<byte[]>(new HttpClientRequest
                {
                    AbsoluteUri = variant.DownloadUrl
                }).ResponseData;
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Unable to get binary data for CmUri: " + binaryComponent.CmUri());
                return null;
            }          
        }     
    }
}
