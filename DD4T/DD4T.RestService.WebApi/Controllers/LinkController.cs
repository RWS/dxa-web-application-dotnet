using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.RestService.WebApi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DD4T.Rest.WebApi.Controllers
{
    [RoutePrefix("link")]
    public class LinkController : ApiController
    {
        private readonly ILinkProvider LinkProvider;
        private readonly ILogger Logger;

        public LinkController(ILinkProvider linkProvider, ILogger logger)
        {
            if (linkProvider == null)
                throw new ArgumentNullException("linkProvider");

            if (logger == null)
                throw new ArgumentNullException("logger");

            LinkProvider = linkProvider;
            Logger = logger;
        }

        [HttpGet]
        [Route("ResolveLink/{publicationId:int}/{componentUri:int}")]
        public IHttpActionResult ResolveLink(int publicationId, int componentUri)
        {
            Logger.Debug("ResolveLink publicationId={0}, componentUri={1}", publicationId, componentUri);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            LinkProvider.PublicationId = publicationId;
            var link = LinkProvider.ResolveLink(componentUri.ToComponentTcmUri(publicationId));

            if (string.IsNullOrEmpty(link))
                return NotFound();

            return Ok<string>(link);
        }
        [HttpGet]
        [Route("ResolveLink/{publicationId:int}/{componentUri:int}/{sourcePageUri:int}/{excludeComponentTemplateUri:int}")]
        public IHttpActionResult ResolveLink(int publicationId, int componentUri, int sourcePageUri, int excludeComponentTemplateUri)
        {
            Logger.Debug("GetContentByUrl publicationId={0}, componentUri={1}, sourcePageUri={2}, excludeComponentTemplateUri{3}", publicationId, componentUri, sourcePageUri, excludeComponentTemplateUri);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            LinkProvider.PublicationId = publicationId;
            var link = LinkProvider.ResolveLink(sourcePageUri.ToPageTcmUri(publicationId), componentUri.ToComponentTcmUri(publicationId), excludeComponentTemplateUri.ToComponentTemplateTcmUri(publicationId));

            if (string.IsNullOrEmpty(link))
                return NotFound();

            return Ok<string>(link);
        }
    }
}
