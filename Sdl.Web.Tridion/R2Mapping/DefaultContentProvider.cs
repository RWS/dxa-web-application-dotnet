using System;
using System.Linq;
using Newtonsoft.Json;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Tridion.ContentDelivery.DynamicContent;
using Tridion.ContentDelivery.DynamicContent.Query;

namespace Sdl.Web.Tridion.R2Mapping
{
    /// <summary>
    /// Default Content Provider implementation (based on DXA R2 Data Model).
    /// </summary>
    public class DefaultContentProvider : IContentProvider
    {
        /// <summary>
        /// Gets a Page Model for a given URL path.
        /// </summary>
        /// <param name="urlPath">The URL path (unescaped).</param>
        /// <param name="localization">The context <see cref="Localization"/>.</param>
        /// <param name="addIncludes">Indicates whether include Pages should be expanded.</param>
        /// <returns>The Page Model.</returns>
        /// <exception cref="DxaItemNotFoundException">If no Page Model exists for the given URL.</exception>
        public PageModel GetPageModel(string urlPath, Localization localization, bool addIncludes = true)
        {
            using (new Tracer(urlPath, localization, addIncludes))
            {
                string canonicalUrlPath = GetCanonicalUrlPath(urlPath);

                string pageContent = GetPageContent(canonicalUrlPath, localization);
                if (pageContent == null)
                {
                    // This may be a SG URL path; try if the index page exists.
                    canonicalUrlPath += Constants.IndexPageUrlSuffix;
                    pageContent = GetPageContent(canonicalUrlPath, localization);
                }

                if (pageContent == null)
                {
                    throw new DxaItemNotFoundException(urlPath, localization.LocalizationId);
                }

                // TODO: View Model Caching
                PageModelData pageModelData = JsonConvert.DeserializeObject<PageModelData>(pageContent, DataModelBinder.SerializerSettings);
                PageModel pageModel = ModelBuilderPipeline.CreatePageModel(pageModelData, addIncludes, localization);
                pageModel.Url = canonicalUrlPath;

                return pageModel;
            }
        }

        public EntityModel GetEntityModel(string id, Localization localization)
        {
            throw new NotImplementedException();
        }

        public StaticContentItem GetStaticContentItem(string urlPath, Localization localization)
        {
            throw new NotImplementedException();
        }

        public void PopulateDynamicList(DynamicList dynamicList, Localization localization)
        {
            throw new NotImplementedException();
        }

        private static string GetCanonicalUrlPath(string urlPath)
        {
            string result = urlPath ?? Constants.IndexPageUrlSuffix;
            if (!result.StartsWith("/"))
            {
                result = "/" + result;
            }
            if (result.EndsWith("/"))
            {
                result += Constants.DefaultExtensionLessPageName;
            }
            else if (result.EndsWith(Constants.DefaultExtension))
            {
                result = result.Substring(0, result.Length - Constants.DefaultExtension.Length);
            }
            return result;
        }

        private static string GetPageContent(string urlPath, Localization localization)
        {
            using (new Tracer(urlPath, localization))
            {
                if (!urlPath.EndsWith(Constants.DefaultExtension))
                {
                    urlPath += Constants.DefaultExtension;
                }

                string escapedUrlPath = Uri.EscapeUriString(urlPath);

                global::Tridion.ContentDelivery.DynamicContent.Query.Query brokerQuery = new global::Tridion.ContentDelivery.DynamicContent.Query.Query
                {
                    Criteria = CriteriaFactory.And(new Criteria[]
                    {
                        new PageURLCriteria(escapedUrlPath),
                        new PublicationCriteria(Convert.ToInt32(localization.Id)),
                        new ItemTypeCriteria(64)
                    })
                };


                string[] pageUris = brokerQuery.ExecuteQuery();
                if (pageUris.Length == 0)
                {
                    return null;
                }
                if (pageUris.Length > 1)
                {
                    throw new DxaException($"Broker Query for Page URL path '{urlPath}' in Publication '{localization.Id}' returned {pageUris.Length} results.");
                }

                PageContentAssembler pageContentAssembler = new PageContentAssembler();
                return pageContentAssembler.GetContent(pageUris[0]);
            }
        }
    }
}
