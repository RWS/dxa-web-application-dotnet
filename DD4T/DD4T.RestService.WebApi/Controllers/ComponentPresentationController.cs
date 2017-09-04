using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Querying;
using DD4T.RestService.WebApi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace DD4T.RestService.WebApi.Controllers
{
    [RoutePrefix("componentpresentation")]
    public class ComponentPresentationController : ApiController
    {
        private readonly IComponentPresentationProvider ComponentPresentationProvider;
        private readonly ILogger Logger;

        public ComponentPresentationController(IComponentPresentationProvider componentPresentationProvider, ILogger logger)
        {
            if (componentPresentationProvider == null)
                throw new ArgumentNullException("componentPresenstationProvider");

            if (logger == null)
                throw new ArgumentNullException("logger");

            Logger = logger;
            ComponentPresentationProvider = componentPresentationProvider;
        }

        [HttpGet]
        [Route("GetContent/{publicationId:int}/{id:int}/{templateId?}")]
        public IHttpActionResult GetContent(int publicationId, int id, int templateId = 0)
        {
            //var templateId = string.Empty;
            Logger.Debug("GetContent  publicationId={0}, componentId={1} tempalteid={2}", publicationId, id, templateId);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            ComponentPresentationProvider.PublicationId = publicationId;

            var content = (templateId == 0) ? ComponentPresentationProvider.GetContent(id.ToComponentTcmUri(publicationId)) :
                    ComponentPresentationProvider.GetContent(id.ToComponentTcmUri(publicationId), templateId.ToComponentTemplateTcmUri(publicationId));

            if (string.IsNullOrEmpty(content))
                return NotFound();

            return Ok<string>(content);
        }

        [HttpGet]
        [Route("GetLastPublishedDate/{publicationId:int}/{id:int}")]
        public IHttpActionResult GetLastPublishedDate(int publicationId, int id)
        {
            Logger.Debug("GetLastPublishedDate  publicationId={0}, componentId={1}", publicationId, id);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            ComponentPresentationProvider.PublicationId = publicationId;
            var content = ComponentPresentationProvider.GetLastPublishedDate(id.ToComponentTcmUri(publicationId));

            return Ok<DateTime>(content);
        }

        [HttpGet]
        [Route("GetContentMultiple/{publicationId:int}/{ids}")]
        //api/componentpresentation/GetContentMultiple/3/1,2,3,4
        public IHttpActionResult GetContentMultiple(int publicationId, [ArrayParam] int[] ids)
        {
            Logger.Debug("GetContentMultiple  publicationId={0}, componentId={1}", publicationId, ids);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            ComponentPresentationProvider.PublicationId = publicationId;

            //Convert the componentid (input is just item_referenceid to tcmuri's
            var tcmuri = ids.Select(compId => compId.ToComponentTcmUri(publicationId)).ToArray<string>();
            var content = ComponentPresentationProvider.GetContentMultiple(tcmuri);

            if (content.Count == 0)
                return NotFound();

            return Ok<List<string>>(content);
        }

        [HttpGet]
        [Route("FindComponents/{publicationId:int}/{queryParameters}")]
        public IHttpActionResult FindComponents(int publicationId, IQuery queryParameters)
        {
            Logger.Debug("FindComponents publicationId={0}, queryParameters={1}", publicationId, queryParameters);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            ComponentPresentationProvider.PublicationId = publicationId;
            var content = ComponentPresentationProvider.FindComponents(queryParameters);

            if (content.Count == 0)
                return NotFound();

            return Ok<IList<string>>(content);
        }

    }
}
