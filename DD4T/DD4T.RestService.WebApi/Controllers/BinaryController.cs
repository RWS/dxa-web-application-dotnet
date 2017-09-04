using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.RestService.WebApi.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DD4T.ContentModel;
using System.Web.Http.Description;

namespace DD4T.RestService.WebApi.Controllers
{
    [RoutePrefix("binary")]
    public class BinaryController : ApiController
    {
        private readonly IBinaryProvider BinaryProvider;
        private readonly ILogger Logger;

        public BinaryController(IBinaryProvider binaryProvider, ILogger logger)
        {
            if (binaryProvider == null)
                throw new ArgumentNullException("binaryProvider");
            if (logger == null)
                throw new ArgumentNullException("logger");

            Logger = logger;
            BinaryProvider = binaryProvider;
        }


        [HttpGet]
        [Route("GetBinaryByUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetBinaryByUri(int publicationId, int id)
        {
            Logger.Debug("GetBinaryByUri  publicationId={0}, componentId={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetBinaryByUri(id.ToComponentTcmUri(publicationId));

            if (binary == null)
                NotFound();

            return Ok<byte[]>(binary);
        }

        [HttpGet]
        [Route("GetBinaryByUrl/{publicationId:int}/{extension}/{*url}")]
        public IHttpActionResult GetBinaryByUrl(int publicationId, string extension, string url)
        {
            Logger.Debug("GetBinaryByUrl  publicationId={0}, url={1}, extension={2}", publicationId, url, extension);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetBinaryByUrl(url.GetUrl(extension));

            if (binary == null)
                NotFound();

            return Ok<byte[]>(binary);
        }

        [HttpGet]
        [Route("GetBinaryStreamByUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetBinaryStreamByUri(int publicationId, int id)
        {
            Logger.Debug("GetBinaryStreamByUri  publicationId={0}, componentId={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetBinaryStreamByUri(id.ToComponentTcmUri(publicationId));

            if (binary == null)
                NotFound();

            return Ok<Stream>(binary);
        }

        [HttpGet]
        [Route("GetBinaryStreamByUrl/{publicationId:int}/{extension}/{*url}")]
        public IHttpActionResult GetBinaryStreamByUrl(int publicationId, string extension, string url)
        {
            Logger.Debug("GetBinaryStreamByUrl  publicationId={0}, url={1}, extension={2}", publicationId, url, extension);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetBinaryStreamByUrl(url.GetUrl(extension));

            if (binary == null)
                NotFound();

            return Ok<Stream>(binary);
        }

        [Obsolete("Use GetBinaryMetaByUri method")]
        [HttpGet]
        [Route("GetLastPublishedDateByUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetLastPublishedDateByUri(int publicationId, int id)
        {
            Logger.Debug("GetLastPublishedDateByUri  publicationId={0}, componentId={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetLastPublishedDateByUri(id.ToComponentTcmUri(publicationId));

            if (binary == null)
                NotFound();

            return Ok<DateTime>(binary);
        }

        [Obsolete("Use GetBinaryMetaByUrl method")]
        [HttpGet]
        [Route("GetLastPublishedDateByUrl/{publicationId:int}/{extension}/{*url}")]
        public IHttpActionResult GetLastPublishedDateByUrl(int publicationId, string extension, string url)
        {
            Logger.Debug("GetLastPublishedDateByUrl  publicationId={0}, url={1}, extension={2}", publicationId, url, extension);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetLastPublishedDateByUrl(url.GetUrl(extension));

            if (binary == null)
                NotFound();

            return Ok<DateTime>(binary);
        }

        [HttpGet]
        [Route("GetBinaryMetaByUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetBinaryMetaByUri(int publicationId, int id)
        {
            Logger.Debug("GetBinaryMetaByUri publicationId={0}, componentId={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetBinaryMetaByUri(id.ToComponentTcmUri(publicationId));

            if (binary == null)
                NotFound();

            return Ok<IBinaryMeta>(binary);
        }

        [HttpGet]
        [Route("GetBinaryMetaByUrl/{publicationId:int}/{extension}/{*url}")]
        [ResponseType(typeof(BinaryMeta))]
        public IHttpActionResult GetBinaryMetaByUrl(int publicationId, string extension, string url)
        {
            Logger.Debug("GetBinaryMetaByUrl publicationId={0}, url={1}, extension={2}", publicationId, url, extension);
            
            BinaryProvider.PublicationId = publicationId;
            IBinaryMeta binaryMeta = BinaryProvider.GetBinaryMetaByUrl(url.GetUrl(extension)) as IBinaryMeta;
                      
            if (binaryMeta == null)
            {
                return NotFound();
            }
            Logger.Debug($"about to return binarymeta {binaryMeta.Id}");


            return Ok(binaryMeta);
        }

        [HttpGet]
        [Route("GetUrlForUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetUrlForUri(int publicationId, int id)
        {
            Logger.Debug("GetUrlForUri publicationId={0}, componentId={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            BinaryProvider.PublicationId = publicationId;
            var binary = BinaryProvider.GetUrlForUri(id.ToComponentTcmUri(publicationId));

            if (binary == null)
                NotFound();

            return Ok<string>(binary);
        }
    }
}
