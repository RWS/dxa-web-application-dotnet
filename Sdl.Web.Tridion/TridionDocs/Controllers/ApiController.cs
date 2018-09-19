using System;
using System.Web.Mvc;
using Sdl.Web.Common;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Controllers;
using Sdl.Web.Mvc.Formats;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.Tridion.TridionDocs.Navigation;
using Sdl.Web.Tridion.TridionDocs.Providers;
using Tridion.ContentDelivery.AmbientData;
using Tridion.ContentDelivery.DynamicContent.Query;

namespace Sdl.Web.Tridion.TridionDocs.Controllers
{
    /// <summary>
    /// Api Controller for Docs content
    /// </summary>
    public class ApiController : BaseController
    {
        private static readonly Uri UserConditionsUri = new Uri("taf:ish:userconditions");
        private static readonly string TocNaventriesMeta = "tocnaventries.generated.value";
        private static readonly string PageConditionsUsedMeta = "conditionsused.generated.value";
        private static readonly string PageLogicalRefObjectId = "ishlogicalref.object.id";      

        [Route("~/api/publications")]
        [HttpGet]
        public virtual ActionResult Publications()
        {
            try
            {
                return Json(new PublicationProvider().PublicationList);
            }
            catch (Exception ex)
            {
                return ServerError(ex);
            }
        }

        [Route("~/api/conditions/{publicationId:int}")]
        [HttpGet]
        public virtual ActionResult Conditions(int publicationId)
        {
            try
            {
                return Json(new ConditionProvider().GetConditions(publicationId));                
            }
            catch (Exception ex)
            {
                return ServerError(ex);
            }
        }

        [Route("~/api/page/{publicationId:int}/{pageId:int}")]
        [HttpGet]
        public virtual ActionResult Page(int publicationId, int pageId)
        {
            try
            {
                var model = EnrichModel(DocsContentProvider.GetPageModel(publicationId, pageId), publicationId);
                return Json(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ServerError(new DxaItemNotFoundException($"Page not found: [{publicationId}] {pageId}/index.html"));
            }
        }

        [Route("~/api/topic/{publicationId:int}/{componentId:int}/{templateId:int}")]
        [HttpGet]
        public virtual ActionResult Topic(int publicationId, int componentId, int templateId)
        {
            try
            {
                var model = EnrichModel(DocsContentProvider.GetEntityModel(publicationId, $"{componentId}-{templateId}"), publicationId);
                return Json(model);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return ServerError(new DxaItemNotFoundException($"Entity not found: [{publicationId}] {componentId}-{templateId}"));
            }
        }
       
        [Route("~/api/topic/{publicationId}/{componentId}/{templateId}")]
        [HttpGet]
        public virtual ActionResult Topic(string publicationId, string componentId, string templateId) 
            => ServerError(new DxaItemNotFoundException($"Entity not found: [{publicationId}] {componentId}-{templateId}"), 400);

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
                ViewModel model = EnrichModel(DocsContentProvider.GetPageModel(publicationId, pageId), publicationId);
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
                StaticContentItem content = DocsContentProvider.GetStaticContentItem(publicationId, binaryId);
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
        [HttpGet]
        public virtual ActionResult RootToc(int publicationId, string conditions = "")
        {
            try
            {
                var localization = DocsContentProvider.CreateDocsLocalization(publicationId);
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
        [HttpGet]
        public virtual ActionResult Toc(int publicationId, string sitemapItemId, string conditions = "",
            bool includeAncestors = false)
        {
            try
            {
                var localization = DocsContentProvider.CreateDocsLocalization(publicationId);
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

        [Route("~/api/sitemap.xml")]
        [HttpGet]
        public virtual ActionResult SitemapXml()
        {
            // Use the common SiteMapXml view for rendering out the xml of all the sitemap items.
            ///return View("SiteMapXml", DDWebAppReactNavigationProvider.SiteMap);
            return new EmptyResult();
        }

        [Route("~/api/toc/{publicationId}/{sitemapItemId}")]
        [HttpGet]
        public virtual ActionResult Toc(string publicationId, string sitemapItemId) => ServerError(null, 400);

        [Route("~/api/pageIdByReference/{publicationId:int}/{ishFieldValue}")]
        [HttpGet]
        public virtual ActionResult TopicIdInTargetPublication(int publicationId, string ishFieldValue)
        {
            try
            {               
                if (!string.IsNullOrEmpty(ishFieldValue))
                {
                    throw new DxaItemNotFoundException(
                        "Unable to use empty 'ishlogicalref.object.id' value as a search criteria.");
                }
                //return Json(GetPageIdByIshLogicalReference(publicationId, ishFieldValue));
                return new EmptyResult();

            }
            catch (Exception ex)
            {
                return ServerError(ex);
            }
        }
        /* TODO: convert to PCA ItemQuery
        public IItem GetPageIdByIshLogicalReference(int publicationId, string ishLogicalRefValue)
        {
            try
            {
                Criteria dateCriteria = new ItemLastPublishedDateCriteria(DefaultPublishData, Criteria.GreaterThanOrEqual);
                CustomMetaKeyCriteria metaKeyCriteria = new CustomMetaKeyCriteria(RefFieldName);
                Criteria refCriteria = new CustomMetaValueCriteria(metaKeyCriteria, ishLogicalRefValue);
                Criteria pubCriteria = new PublicationCriteria(publicationId);
                Criteria itemType = new ItemTypeCriteria((int)ItemType.Page);
                Criteria composite = new AndCriteria(new[] { dateCriteria, refCriteria, itemType, pubCriteria });

                global::Tridion.ContentDelivery.DynamicContent.Query.Query query = new global::Tridion.ContentDelivery.DynamicContent.Query.Query(composite);
                IItem[] items = query.ExecuteEntityQuery();
                if (items == null || items.Length == 0)
                {
                    return new ItemImpl();
                }

                if (items.Length > 1)
                {
                    throw new ApiException($"Too many page Ids found in publication with logical ref value {ishLogicalRefValue}");
                }

                return items[0];
            }
            catch (Exception)
            {
                throw new DxaItemNotFoundException($"Page reference by ishlogicalref.object.id = {ishLogicalRefValue} not found in publication {publicationId}.");
            }
        }
        */
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
            var page = client.GetPage(ContentNamespace.Docs, publicationId, int.Parse(pageModel.Id),
                $"requiredMeta:{TocNaventriesMeta},{PageConditionsUsedMeta},{PageLogicalRefObjectId}", null);
            if (page?.CustomMetas == null) return model;
            foreach (var x in page.CustomMetas.Edges)
            {
                if (TocNaventriesMeta.Equals(x.Node.Key))
                {
                    pageModel.Meta.Add(TocNaventriesMeta, x.Node.Value);
                }
                if (PageConditionsUsedMeta.Equals(x.Node.Key))
                {
                    pageModel.Meta.Add(PageLogicalRefObjectId, x.Node.Value);
                }
                if (PageLogicalRefObjectId.Equals(x.Node.Key) && !pageModel.Meta.ContainsKey(PageLogicalRefObjectId))
                {
                    pageModel.Meta.Add(PageLogicalRefObjectId, x.Node.Value);
                }
            }          
            return model;
        }       
    }
}
