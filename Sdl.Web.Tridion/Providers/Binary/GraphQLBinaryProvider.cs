using System;
using System.Threading;
using System.Threading.Tasks;
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

        public async Task<DateTime> GetBinaryLastPublishedDateAsync(ILocalization localization, string urlPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = await client.GetBinaryComponentAsync(GetNamespace(localization), int.Parse(localization.Id), urlPath, null, cancellationToken);
            return binary == null ? DateTime.MinValue : DateTime.ParseExact(binary.InitialPublishDate, DateTimeFormat, null);
        }

        public DateTime GetBinaryLastPublishedDate(ILocalization localization, int binaryId)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), binaryId, null);
            return binary == null ? DateTime.MinValue : DateTime.ParseExact(binary.InitialPublishDate, DateTimeFormat, null);
        }

        public async Task<DateTime> GetBinaryLastPublishedDateAsync(ILocalization localization, int binaryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = await client.GetBinaryComponentAsync(GetNamespace(localization), int.Parse(localization.Id), binaryId, null, cancellationToken);
            return binary == null ? DateTime.MinValue : DateTime.ParseExact(binary.InitialPublishDate, DateTimeFormat, null);
        }

        public Tuple<byte[],string> GetBinary(ILocalization localization, int binaryId)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), binaryId, null);
            var data = GetBinaryData(client, binary);
            if (data == null) throw new DxaItemNotFoundException(binaryId.ToString(), localization.Id);
            return data;
        }

        public async Task<Tuple<byte[], string>> GetBinaryAsync(ILocalization localization, int binaryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = await client.GetBinaryComponentAsync(GetNamespace(localization), int.Parse(localization.Id), binaryId, null, cancellationToken);
            var data = await GetBinaryDataAsync(client, binary, cancellationToken);
            if (data == null) throw new DxaItemNotFoundException(binaryId.ToString(), localization.Id);
            return data;
        }             

        public Tuple<byte[],string> GetBinary(ILocalization localization, string urlPath)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = client.GetBinaryComponent(GetNamespace(localization), int.Parse(localization.Id), urlPath, null);
            var data = GetBinaryData(client, binary);
            if(data == null) throw new DxaItemNotFoundException(urlPath, localization.Id);
            return data;
        }

        public async Task<Tuple<byte[], string>> GetBinaryAsync(ILocalization localization, string urlPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            var client = PCAClientFactory.Instance.CreateClient();
            var binary = await client.GetBinaryComponentAsync(GetNamespace(localization), int.Parse(localization.Id), urlPath, null, cancellationToken);
            var data = await GetBinaryDataAsync(client, binary, cancellationToken);
            if (data == null) throw new DxaItemNotFoundException(urlPath, localization.Id);
            return data;
        }

        protected virtual Tuple<byte[],string> GetBinaryData(IGraphQLClient client, BinaryComponent binaryComponent)
        {
            if (binaryComponent == null) return null;
          
            if (binaryComponent?.Variants == null)
            {
                Log.Error("Unable to get binary data for CmUri: " + binaryComponent.CmUri());
                return null;
            }
            var variant = binaryComponent.Variants.Edges[0].Node;
            try
            {
                Log.Debug("Attempting to get binary at : " + variant.DownloadUrl);
                return new Tuple<byte[], string>(client.HttpClient.Execute<byte[]>(new HttpClientRequest
                {
                    AbsoluteUri = variant.DownloadUrl
                }).ResponseData, variant.Path);
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Unable to get binary data for CmUri: " + binaryComponent.CmUri());
                return null;
            }          
        }

        protected virtual async Task<Tuple<byte[], string>> GetBinaryDataAsync(IGraphQLClient client, BinaryComponent binaryComponent, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (binaryComponent == null) return null;

            if (binaryComponent?.Variants == null)
            {
                Log.Error("Unable to get binary data for CmUri: " + binaryComponent.CmUri());
                return null;
            }
            var variant = binaryComponent.Variants.Edges[0].Node;
            try
            {
                Log.Debug("Attempting to get binary at : " + variant.DownloadUrl);
                var data = await client.HttpClient.ExecuteAsync<byte[]>(new HttpClientRequest
                {
                    AbsoluteUri = variant.DownloadUrl
                }, cancellationToken);

                return new Tuple<byte[], string>(data.ResponseData, variant.Path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to get binary data for CmUri: " + binaryComponent.CmUri());
                return null;
            }
        }
    }
}
