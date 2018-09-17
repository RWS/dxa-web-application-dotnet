using System;
using System.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Controllers;
using Sdl.Web.Mvc.Formats;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.Tridion.TridionDocs.Localization;
using Sdl.Web.Tridion.TridionDocs.Navigation;
using Tridion.ContentDelivery.AmbientData;

namespace Sdl.Web.Tridion.TridionDocs.Controllers
{
    public class ApiController : BaseController
    {
        private static readonly Uri UserConditionsUri = new Uri("taf:ish:userconditions");
        private static readonly string TocNaventriesMeta = "tocnaventries.generated.value";
        private static readonly string PageConditionsUsedMeta = "conditionsused.generated.value";
        private static readonly string PageLogicalRefObjectId = "ishlogicalref.object.id";

        protected virtual ILocalization CreateDocsLocalization(int publicationId) => new DocsLocalization {Id = publicationId.ToString()};

        [Route("~/api/page/{publicationId:int}/{pageId:int}")]
        [HttpGet]
        public virtual ActionResult Page(int publicationId, int pageId)
        {
            try
            {
                var model = EnrichModel(ContentProvider.GetPageModel(pageId, CreateDocsLocalization(publicationId)), publicationId);
                return Json(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ServerError(new DxaItemNotFoundException($"Page not found: [{publicationId}] {pageId}/index.html"));
            }
        }

        [Route("~/api/page/{publicationId}/{pageId}")]
        [HttpGet]
        public virtual ActionResult Page(string publicationId, string pageId) 
            => ServerError(new DxaItemNotFoundException($"Page not found: [{publicationId}] {pageId}/index.html"), 400);

        [Route("~/api/page/{publicationId:int}/{pageId:int}/{*content}")]
        [HttpPost]
        public virtual ActionResult Page(int publicationId, int pageId, string content)
        {
            try
            {
                string conditions = Request.QueryString["conditions"];
                if (!string.IsNullOrEmpty(conditions))
                {
                    AmbientDataContext.CurrentClaimStore.Put(UserConditionsUri, conditions);
                }
                ViewModel model = EnrichModel(ContentProvider.GetPageModel(pageId, CreateDocsLocalization(publicationId)), publicationId);
                return Json(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ServerError(ex);
            }
        }

        [Route("~/binary/{publicationId:int}/{binaryId:int}/{*content}")]
        [Route("~/api/binary/{publicationId:int}/{binaryId:int}/{*content}")]
        [HttpGet]
        [FormatData]
        public virtual ActionResult Binary(int publicationId, int binaryId)
        {
            try
            {
                StaticContentItem content = ContentProvider.GetStaticContentItem(binaryId,
                    CreateDocsLocalization(publicationId));
                return new FileStreamResult(content.GetContentStream(), content.ContentType);
            }
            catch (Exception ex)
            {
                return ServerError(ex);
            }
        }

        [Route("~/binary/{publicationId}/{binaryId}")]
        [Route("~/api/binary/{publicationId}/{binaryId}")]
        [HttpGet]
        [FormatData]
        public virtual ActionResult Binary(string publicationId, string binaryId) => ServerError(null, 400);

        [Route("~/api/toc/{publicationId:int}")]
        public virtual ActionResult RootToc(int publicationId, string conditions = "")
        {
            try
            {
                var localization = CreateDocsLocalization(publicationId);
                if (!string.IsNullOrEmpty(conditions))
                {
                    AmbientDataContext.CurrentClaimStore.Put(UserConditionsUri, conditions);
                }
                TocProvider tocProvider = new TocProvider();
                return Json(tocProvider.GetToc(localization));
            }
            catch (Exception ex)
            {
                return ServerError(ex);
            }
        }

        [Route("~/api/toc/{publicationId:int}/{sitemapItemId}")]
        public virtual ActionResult Toc(int publicationId, string sitemapItemId, string conditions = "",
            bool includeAncestors = false)
        {
            try
            {
                var localization = CreateDocsLocalization(publicationId);
                if (!string.IsNullOrEmpty(conditions))
                {
                    AmbientDataContext.CurrentClaimStore.Put(UserConditionsUri, conditions);
                }
                TocProvider tocProvider = new TocProvider();
                var sitemapItems = tocProvider.GetToc(localization, sitemapItemId, includeAncestors);
                return Json(sitemapItems);
            }
            catch (Exception ex)
            {
                return ServerError(ex);
            }
        }

        [Route("~/api/toc/{publicationId}/{sitemapItemId}")]
        public virtual ActionResult Toc(string publicationId, string sitemapItemId) => ServerError(null, 400);

        public ActionResult ServerError(Exception ex, int statusCode = 404)
        {
            Response.StatusCode = statusCode;
            if (ex == null) return new EmptyResult();
            if (ex.InnerException != null) ex = ex.InnerException;
            return Content("{ \"Message\": \"" + ex.Message + "\" }", "application/json");
        }

        protected virtual ViewModel EnrichModel(ViewModel model, int publicationId)
        {
            PageModel pageModel = model as PageModel;
            if (pageModel == null) return model;
            var client = PCAClient.PCAClientFactory.Instance.CreateClient();
            var page = client.GetPage(ContentNamespace.Docs, publicationId, int.Parse(pageModel.Id), null,
                $"requiredMeta:{TocNaventriesMeta},{PageConditionsUsedMeta},{PageLogicalRefObjectId}");
            if (page?.CustomMetas == null) return model;
            foreach (var x in page.CustomMetas.Edges)
            {
                if (TocNaventriesMeta.Equals(x.Node.Key))
                {
                    pageModel.Meta.Add(TocNaventriesMeta, x.Node.Key);
                }
                if (PageConditionsUsedMeta.Equals(x.Node.Key))
                {
                    pageModel.Meta.Add(PageLogicalRefObjectId, x.Node.Value);
                }
                if (PageLogicalRefObjectId.Equals(x.Node.Key))
                {
                    pageModel.Meta.Add(PageLogicalRefObjectId, x.Node.Value);
                }
            }          
            return model;
        }       
    }
}
