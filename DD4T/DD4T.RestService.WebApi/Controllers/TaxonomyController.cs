using DD4T.ContentModel;
using DD4T.ContentModel.Contracts.Providers;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.RestService.WebApi.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DD4T.RestService.WebApi.Controllers
{
    [RoutePrefix("taxonomy")]
    public class TaxonomyController : ApiController
    {
        private readonly ITaxonomyProvider TaxonomyProvider;
        private readonly ILogger Logger;

        public TaxonomyController(ITaxonomyProvider taxonomyProvider, ILogger logger)
        {
            if (taxonomyProvider == null)
                throw new ArgumentNullException("taxonomyProvider");

            if (logger == null)
                throw new ArgumentNullException("logger");

            TaxonomyProvider = taxonomyProvider;
            Logger = logger;
        }

        [HttpGet]
        [Route("GetKeyword/{publicationId:int}/{categoryUriToLookIn:int}/{keywordName}")]
        public IHttpActionResult GetKeyword(int publicationId, int categoryUriToLookIn, string keywordName)
        {
            Logger.Debug("ResolveLink publicationId={0}, categoryUriToLookIn={1}, keywordName={2}", publicationId, categoryUriToLookIn, keywordName);
            if (publicationId == 0)
                return BadRequest(Messages.EmptyPublicationId);

            TaxonomyProvider.PublicationId = publicationId;
            var keyword = TaxonomyProvider.GetKeyword(categoryUriToLookIn.ToCategoryTcmUri(publicationId), keywordName);

            return Ok<IKeyword>(keyword);

        }


    }
}
