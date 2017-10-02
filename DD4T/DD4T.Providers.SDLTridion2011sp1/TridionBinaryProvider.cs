using System;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.Meta;
using DD4T.ContentModel;
using DD4T.ContentModel.Exceptions;
//using DD4T.Utils;
using System.Collections.Generic;
using System.Web;
using DD4T.ContentModel.Contracts.Providers;
using System.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Collections;
using System.IO;

namespace DD4T.Providers.SDLTridion2011sp1
{
    /// <summary>
    /// Provide access to binaries in a Tridion broker instance
    /// </summary>
    public class TridionBinaryProvider : BaseProvider, IBinaryProvider
    {
        public TridionBinaryProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {

        }
        #region public static

        public static string SqlQuery = "SELECT BC.CONTENT FROM BINARY_CONTENT BC, BINARYVARIANTS BV WHERE BV.URL = @url AND BC.BINARY_ID = BV.BINARY_ID AND BC.PUBLICATION_ID = BV.PUBLICATION_ID AND BC.VARIANT_ID = BV.VARIANT_ID"; 

        #endregion

        #region private stuff
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager.AppSettings["BinaryProviderBrokerConnectionString"];
            }
        }

        private static IDictionary<string, DateTime> lastPublishedDates = new Dictionary<string, DateTime>();

        // NOTE: the BinaryFactory referenced here is part of the Tridion.ContentDelivery namespace
        // Not to be confused with the BinaryFactory from DD4T. The usage chain is:
        // DD4T.Factories.BinaryFactory >>> DD4T.Providers.*.TridionBinaryProvider >>> Tridion.ContentDelivery.DynamicContent.BinaryFactory
        private BinaryFactory _tridionBinaryFactory = null;
        private BinaryFactory TridionBinaryFactory
        {
            get
            {
                if (_tridionBinaryFactory == null)
                    _tridionBinaryFactory = new BinaryFactory();
                return _tridionBinaryFactory;
            }
        }


        private BinaryMetaFactory _binaryMetaFactory = null;
        private BinaryMetaFactory BinaryMetaFactory
        {
            get
            {
                if (_binaryMetaFactory == null)
                    _binaryMetaFactory = new BinaryMetaFactory();
                return _binaryMetaFactory;
            }
        }

        

        private Dictionary<int,ComponentMetaFactory > _tridionComponentMetaFactories = new Dictionary<int,ComponentMetaFactory>();


        private object lock1 = new object();
        private ComponentMetaFactory GetTridionComponentMetaFactory(int publicationId)
        {
            if (_tridionComponentMetaFactories.ContainsKey(publicationId))
                return _tridionComponentMetaFactories[publicationId];
            lock (lock1)
            {
                if (!_tridionComponentMetaFactories.ContainsKey(publicationId)) // we must test again, because in the mean time another thread might have added a record to the dictionary!
                    _tridionComponentMetaFactories.Add(publicationId, new ComponentMetaFactory(publicationId));
            }
            return _tridionComponentMetaFactories[publicationId];
        }

        #endregion

        #region IBinaryProvider Members

        public byte[] GetBinaryByUri(string uri)
        {
            Tridion.ContentDelivery.DynamicContent.BinaryFactory factory = new BinaryFactory();
            BinaryData binaryData = factory.GetBinary(uri.ToString());
            return binaryData == null ? null : binaryData.Bytes;
        }

        public byte[] GetBinaryByUrl(string url)
        {
            string encodedUrl = HttpUtility.UrlPathEncode(url); // ?? why here? why now?


            IList metas = null;
            Tridion.ContentDelivery.Meta.BinaryMeta binaryMeta = null;
            if (this.PublicationId == 0)
            {
                metas = BinaryMetaFactory.GetMetaByUrl(encodedUrl);
                if (metas.Count == 0)
                {
                    throw new BinaryNotFoundException();
                }
                binaryMeta = metas[0] as Tridion.ContentDelivery.Meta.BinaryMeta;
            }
            else
            {
                binaryMeta = BinaryMetaFactory.GetMetaByUrl(this.PublicationId, encodedUrl);
                if (binaryMeta == null)
                    throw new BinaryNotFoundException();
            }
            TcmUri uri = new TcmUri(binaryMeta.PublicationId,binaryMeta.Id,16,0);

            Tridion.ContentDelivery.DynamicContent.BinaryFactory factory = new BinaryFactory();

            BinaryData binaryData = string.IsNullOrEmpty(binaryMeta.VariantId) ? factory.GetBinary(uri.ToString()) : factory.GetBinary(uri.ToString(),binaryMeta.VariantId);
            return binaryData == null ? null : binaryData.Bytes;
        }

       
        public DateTime GetLastPublishedDateByUrl(string url)
        {
            string encodedUrl = HttpUtility.UrlPathEncode(url); // ?? why here? why now?

            Tridion.ContentDelivery.Meta.BinaryMeta binaryMeta = null;
            if (this.PublicationId == 0)
            {
                IList metas = BinaryMetaFactory.GetMetaByUrl(encodedUrl);
                if (metas.Count == 0)
                    return DateTime.MinValue.AddSeconds(1); // TODO: use nullable type

                binaryMeta = metas[0] as Tridion.ContentDelivery.Meta.BinaryMeta;
            }
            else
            {
                binaryMeta = BinaryMetaFactory.GetMetaByUrl(this.PublicationId, encodedUrl);
            }

            Tridion.ContentDelivery.Meta.IComponentMeta componentMeta = GetTridionComponentMetaFactory(binaryMeta.PublicationId).GetMeta(binaryMeta.Id);
            return componentMeta == null ? DateTime.MinValue : componentMeta.LastPublicationDate;
        }

        public DateTime GetLastPublishedDateByUri(string uri)
        {
            TcmUri tcmUri = new TcmUri(uri);
            Tridion.ContentDelivery.Meta.IComponentMeta componentMeta = GetTridionComponentMetaFactory(tcmUri.PublicationId).GetMeta(tcmUri.ItemId);
            return componentMeta == null ? DateTime.MinValue : componentMeta.LastPublicationDate;
        }


        [Obsolete("Retrieving binaries as a stream will be removed from the next version of DD4T")]
        public System.IO.Stream GetBinaryStreamByUri(string uri)
        {
            throw new NotImplementedException();
        }

        [Obsolete("Retrieving binaries as a stream will be removed from the next version of DD4T")]
        public System.IO.Stream GetBinaryStreamByUrl(string url)
        {
            SqlReaderStream stream = null;
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                SqlCommand cmd = new SqlCommand(SqlQuery, cn);
                cmd.Parameters.Add("@url", SqlDbType.VarChar, 255); // note: the length of the URL parameter must equal the length of the BINARY_VARIANT.PATH column in the broker database
                cmd.Parameters["@url"].Value = url;
                cn.Open();
                //CommandBehavior.SequentialAccess avoids loading the entire BLOB in-memory.
                SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
                if (false == reader.Read())
                {
                    reader.Dispose();
                    return null;
                }
                stream = new SqlReaderStream(reader, 0);
            }
            return stream;
        }

        public IBinaryMeta GetBinaryMetaByUri(string uri)
        {
            Tridion.ContentDelivery.Meta.BinaryMeta binaryMeta = null;
            binaryMeta = BinaryMetaFactory.GetMeta(uri);
            if (binaryMeta == null)
            {
                LoggerService.Debug("cannot find binary with uri " + uri);
                return null;
            }
            DD4T.ContentModel.BinaryMeta bm = new ContentModel.BinaryMeta()
            {
                Id = uri,
                VariantId = binaryMeta.VariantId,
            };
            Tridion.ContentDelivery.Meta.IComponentMeta componentMeta = GetTridionComponentMetaFactory(binaryMeta.PublicationId).GetMeta(binaryMeta.Id);
            if (componentMeta == null)
            {
                LoggerService.Debug("no component metadata found for binary with uri " + uri);
                bm.HasLastPublishedDate = false;
                return bm;
            }
            bm.HasLastPublishedDate = true;
            bm.LastPublishedDate = componentMeta.LastPublicationDate;
            LoggerService.Debug(string.Format("returning binary for uri {0} with the following metadata: Id = {1}, VariantId = {2}, HasLastPublishDate = {3}, lastPublishDate = {4}", uri, bm.Id, bm.VariantId, bm.HasLastPublishedDate, bm.LastPublishedDate));
            return bm;
        }

        public IBinaryMeta GetBinaryMetaByUrl(string url)
        {
            LoggerService.Debug($"started GetBinaryMetaByUrl for url {url} with publication id {PublicationId}");
            string encodedUrl = HttpUtility.UrlPathEncode(url); // ?? why here? why now?
            LoggerService.Debug($"using encodedUrl: {encodedUrl}");
            Tridion.ContentDelivery.Meta.BinaryMeta binaryMeta = null;
            if (this.PublicationId == 0)
            {
                IList metas = BinaryMetaFactory.GetMetaByUrl(encodedUrl);
                if (metas.Count == 0)
                    return null;

                binaryMeta = metas[0] as Tridion.ContentDelivery.Meta.BinaryMeta;
            }
            else
            {
                binaryMeta = BinaryMetaFactory.GetMetaByUrl(this.PublicationId, encodedUrl);
            }
            if (binaryMeta == null)
            {
                LoggerService.Debug("cannot find binary with URL " + url);
                return null;
            }
            LoggerService.Debug($"found binarymeta with ID {binaryMeta.Id}");

            string uri = string.Format("tcm:{0}-{1}", binaryMeta.PublicationId, binaryMeta.Id);
            DD4T.ContentModel.BinaryMeta bm = new ContentModel.BinaryMeta()
            {
                Id = uri,
                VariantId = binaryMeta.VariantId,
            };
            LoggerService.Debug("about to call ComponentMetaFactory.GetMeta");
            Tridion.ContentDelivery.Meta.IComponentMeta componentMeta = GetTridionComponentMetaFactory(binaryMeta.PublicationId).GetMeta(binaryMeta.Id);
            if (componentMeta == null)
            {
                LoggerService.Debug("no component metadata found for binary with url " + url);
                bm.HasLastPublishedDate = false;
                return bm;
            }
            LoggerService.Debug($"found component meta with LastPublishDate {componentMeta.LastPublicationDate}");
            bm.HasLastPublishedDate = true;
            bm.LastPublishedDate = componentMeta.LastPublicationDate;
            LoggerService.Debug(string.Format("returning binary for url {0} with the following metadata: Id = {1}, VariantId = {2}, HasLastPublishDate = {3}, lastPublishDate = {4}", url, bm.Id, bm.VariantId, bm.HasLastPublishedDate, bm.LastPublishedDate));
            return bm;
        }


        #endregion



        public string GetUrlForUri(string uri)
        {
            var item = BinaryMetaFactory.GetMeta(uri);
            return item == null ? string.Empty : item.UrlPath; // TODO: test this change (with urls that exist and urls that don't!)
        }
    }
    internal class SqlReaderStream : Stream
    {
        private SqlDataReader reader;
        private int columnIndex;
        private long position;

        public SqlReaderStream(
            SqlDataReader reader,
            int columnIndex)
        {
            this.reader = reader;
            this.columnIndex = columnIndex;
        }

        public override long Position
        {
            get { return position; }
            set { throw new NotImplementedException(); }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            long bytesRead = reader.GetBytes(columnIndex, position, buffer, offset, count);
            position += bytesRead;
            return (int)bytesRead;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Length
        {
            get { throw new NotImplementedException(); }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && null != reader)
            {
                reader.Dispose();
                reader = null;
            }
            base.Dispose(disposing);
        }
    }
}
