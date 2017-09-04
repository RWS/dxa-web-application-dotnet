using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.RestService.WebApi.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DD4T.RestService.WebApi.Controllers
{
    [RoutePrefix("page")]
    public class PageController : ApiController
    {
        private readonly IPageProvider PageProvider;
        private readonly ILogger Logger;

        public PageController(IPageProvider pageProvider, ILogger logger)
        {
            if (pageProvider == null)
                throw new ArgumentNullException("pageProvider");

            if (logger == null)
                throw new ArgumentNullException("logger");

            PageProvider = pageProvider;
            Logger = logger;
        }

        [HttpGet]
        [Route("GetContentByUrl/{publicationId:int}/{extension}/{*url}")]
        public IHttpActionResult GetContentByUrl(int publicationId, string extension, string url)
        {
            Logger.Debug("PageController.GetContentByUrl publicationId={0}, Url={1}, extension={2}", publicationId, url, extension);

            PageProvider.PublicationId = publicationId;
            var content = PageProvider.GetContentByUrl(url.GetUrl(extension));

            if (string.IsNullOrEmpty(content))
                return NotFound();

            return Ok<string>(content);
        }

        [HttpGet]
        [Route("GetContentByUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetContentByUri(int publicationId, int id)
        {
            Logger.Debug("PageController.GetContentByUri publicationId={0}, Url={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            PageProvider.PublicationId = publicationId;
            var content = PageProvider.GetContentByUri(id.ToPageTcmUri(publicationId));
 
            if (string.IsNullOrEmpty(content))
                return NotFound();

            return Ok<string>(content);
        }

        [HttpGet]
        [Route("GetLastPublishedDateByUrl/{publicationId:int}/{extension}/{*url}")]
        public IHttpActionResult GetLastPublishedDateByUrl(int publicationId, string extension, string url)
        {
            Logger.Debug("PageController.GetLastPublishedDateByUrl publicationId={0}, Url={1}, Extension={2}", publicationId, url, extension);

            PageProvider.PublicationId = publicationId;
            var content = PageProvider.GetLastPublishedDateByUrl(url.GetUrl(extension));

            return Ok<DateTime>(content);
        }

        [HttpGet]
        [Route("GetLastPublishedDateByUri/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetLastPublishedDateByUri(int publicationId, int id)
        {
            Logger.Debug("PageController.GetLastPublishedDateByUri publicationId={0}, Url={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            PageProvider.PublicationId = publicationId;
            var content = PageProvider.GetLastPublishedDateByUri(id.ToPageTcmUri(publicationId));

            return Ok<DateTime>(content);
        }
    }
}
