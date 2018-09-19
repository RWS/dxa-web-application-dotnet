using System;
using System.Threading;
using System.Threading.Tasks;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.Meta;

namespace Sdl.Web.Tridion.Providers.Binary
{
    /// <summary>
    /// Binary Provider
    /// </summary>
    public class CILBinaryProvider : IBinaryProvider
    {
        public DateTime GetBinaryLastPublishedDate(ILocalization localization, string urlPath)
        {
            BinaryMeta binaryMeta = GetBinaryMeta(localization, urlPath);
            if (binaryMeta == null || !binaryMeta.IsComponent)
            {
                return DateTime.MinValue;
            }
            ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(int.Parse(localization.Id));
            IComponentMeta componentMeta = componentMetaFactory.GetMeta(binaryMeta.Id);
            return componentMeta.LastPublicationDate;
        }

        public async Task<DateTime> GetBinaryLastPublishedDateAsync(ILocalization localization, string urlPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public DateTime GetBinaryLastPublishedDate(ILocalization localization, int binaryId)
        {
            BinaryMeta binaryMeta = GetBinaryMeta(localization, binaryId);
            if (binaryMeta == null || !binaryMeta.IsComponent)
            {
                return DateTime.MinValue;
            }
            ComponentMetaFactory componentMetaFactory = new ComponentMetaFactory(int.Parse(localization.Id));
            IComponentMeta componentMeta = componentMetaFactory.GetMeta(binaryMeta.Id);
            return componentMeta.LastPublicationDate;
        }

        public Task<DateTime> GetBinaryLastPublishedDateAsync(ILocalization localization, int binaryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Tuple<byte[],string> GetBinary(ILocalization localization, string urlPath)
        {
            // Binary does not exist or cached binary is out-of-date
            BinaryMeta binaryMeta = GetBinaryMeta(localization, urlPath);
            if (binaryMeta == null)
            {
                throw new DxaItemNotFoundException(urlPath, localization.Id);
            }
            BinaryFactory binaryFactory = new BinaryFactory();
            BinaryData binaryData = binaryFactory.GetBinary(int.Parse(localization.Id), binaryMeta.Id, binaryMeta.VariantId);
            return new Tuple<byte[], string>(binaryData.Bytes, binaryMeta.Path);
        }

        public Tuple<byte[],string> GetBinary(ILocalization localization, int binaryId)
        {
            BinaryMeta binaryMeta = GetBinaryMeta(localization, binaryId);
            if (binaryMeta == null)
            {
                throw new DxaItemNotFoundException(binaryId.ToString(), localization.Id);
            }
            BinaryFactory binaryFactory = new BinaryFactory();
            BinaryData binaryData = binaryFactory.GetBinary(int.Parse(localization.Id), binaryMeta.Id, binaryMeta.VariantId);
            return new Tuple<byte[], string>(binaryData.Bytes, binaryMeta.Path);
        }

        public Task<Tuple<byte[], string>> GetBinaryAsync(ILocalization localization, string urlPath, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        public Task<Tuple<byte[], string>> GetBinaryAsync(ILocalization localization, int binaryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }

        private static BinaryMeta GetBinaryMeta(ILocalization localization, int binaryId)
        {
            BinaryMetaFactory binaryMetaFactory = new BinaryMetaFactory();
            return binaryMetaFactory.GetMeta($"{localization.CmUriScheme}:{localization.Id}-{binaryId}");
        }

        private static BinaryMeta GetBinaryMeta(ILocalization localization, string urlPath)
        {
            BinaryMetaFactory binaryMetaFactory = new BinaryMetaFactory();
            return binaryMetaFactory.GetMetaByUrl(int.Parse(localization.Id), urlPath);
        }
    }
}
