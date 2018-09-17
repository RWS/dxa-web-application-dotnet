using System;
using System.Collections.Generic;
using System.Linq;
using Sdl.Web.Common;
using Sdl.Web.Common.Logging;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.Tridion.PCAClient;

namespace Sdl.Web.Tridion.TridionDocs.Providers
{
    /// <summary>
    /// Publication Provider
    /// </summary>
    public class PublicationProvider
    {
        protected static readonly string DateTimeFormat = "MM/dd/yyyy HH:mm:ss";

        private static readonly string PublicationTitleMeta = "publicationtitle.generated.value";
        private static readonly string PublicationProductfamilynameMeta = "FISHPRODUCTFAMILYNAME.logical.value";
        private static readonly string PublicationProductreleasenameMeta = "FISHPRODUCTRELEASENAME.version.value";
        private static readonly string PublicationVersionrefMeta = "ishversionref.object.id";
        private static readonly string PublicationLangMeta = "FISHPUBLNGCOMBINATION.lng.value";
        private static readonly string PublicationOnlineStatusMeta = "FISHDITADLVRREMOTESTATUS.lng.element";
        private static readonly string PublicationOnlineValue = "VDITADLVRREMOTESTATUSONLINE";
        private static readonly string PublicationCratedonMeta = "CREATED-ON.version.value";
        private static readonly string PublicationVersionMeta = "VERSION.version.value";
        private static readonly string PublicationLogicalId = "ishref.object.value";

        private static readonly string CustomMetaFilter = $"requiredMeta:{PublicationTitleMeta},{PublicationProductfamilynameMeta},{PublicationProductreleasenameMeta},{PublicationVersionrefMeta},{PublicationLangMeta},{PublicationOnlineStatusMeta},{PublicationCratedonMeta},{PublicationVersionMeta},{PublicationLogicalId}";

        public List<Model.Publication> PublicationList
        {
            get
            {
                try
                {
                    var client = PCAClientFactory.Instance.CreateClient();
                    var publications = client.GetPublications(ContentNamespace.Docs, new Pagination(), null, null, CustomMetaFilter);
                    return (from x in publications.Edges where IsPublicationOnline(x.Node) select BuildPublicationFrom(x.Node)).ToList();
                }
                catch (Exception e)
                {
                    throw new DxaItemNotFoundException("Unable to fetch list of publications.");
                }
            }
        }

        public bool IsPublicationOnline(PublicContentApi.ContentModel.Publication publication)
        {
            var customMeta = publication.CustomMetas;
            if (customMeta == null) return false;
            try
            {
                return customMeta.Edges.Where(x => PublicationOnlineStatusMeta.Equals(x.Node.Key)).Any(x => PublicationOnlineValue.Equals(x.Node.Value));
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void CheckPublicationOnline(int publicationId)
        {
            var client = PCAClientFactory.Instance.CreateClient();
            bool isOffline = false;
            try
            {
                var publication = client.GetPublication(ContentNamespace.Docs, publicationId, null, $"requiredMeta:{PublicationOnlineStatusMeta}");
                isOffline = publication.CustomMetas == null || publication.CustomMetas.Edges.Count == 0 ||
                            !PublicationOnlineValue.Equals(publication.CustomMetas.Edges[0].Node.Value);
            }
            catch (Exception)
            {
                Log.Error("Couldn't find publication metadata for id: " + publicationId);
            }
            if (isOffline)
            {
                throw new DxaItemNotFoundException($"Unable to find publication {publicationId}");
            }
        }

        private Model.Publication BuildPublicationFrom(PublicContentApi.ContentModel.Publication publication)
        {
            Model.Publication result = new Model.Publication
            {
                Id = publication.ItemId.ToString(),
                Title = publication.Title
            };
            var customMeta = publication.CustomMetas;
            if (customMeta == null) return result;
            result.ProductFamily = null;
            result.ProductReleaseVersion = null;
            foreach (var x in customMeta.Edges)
            {
                if (x.Node.Key == PublicationTitleMeta)
                    result.Title = x.Node.Value;

                if (x.Node.Key == PublicationLangMeta)
                    result.Language = x.Node.Value;

                if (x.Node.Key == PublicationProductfamilynameMeta)
                {
                    if (result.ProductFamily == null) result.ProductFamily = new List<string>();
                    result.ProductFamily.Add(x.Node.Value);
                }

                if (x.Node.Key == PublicationProductreleasenameMeta)
                {
                    if (result.ProductReleaseVersion == null) result.ProductReleaseVersion = new List<string>();
                    result.ProductReleaseVersion.Add(x.Node.Value);
                }

                if (x.Node.Key == PublicationCratedonMeta)
                {
                    result.CreatedOn = DateTime.ParseExact(x.Node.Value, DateTimeFormat, null);
                }

                if (x.Node.Key == PublicationVersionMeta)
                {
                    result.Version = x.Node.Value;
                }

                if (x.Node.Key == PublicationVersionrefMeta)
                {
                    result.VersionRef = x.Node.Value;
                }

                if (x.Node.Key == PublicationLogicalId)
                {
                    result.LogicalId = x.Node.Value;
                }
            }

            return result;
        }
    }
}
