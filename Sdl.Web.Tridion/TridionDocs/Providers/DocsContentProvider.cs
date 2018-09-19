using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.Tridion.TridionDocs.Localization;

namespace Sdl.Web.Tridion.TridionDocs.Providers
{
    public static class DocsContentProvider
    {
        public static ILocalization CreateDocsLocalization(int publicationId) => new DocsLocalization { Id = publicationId.ToString() };

        private static IContentProvider ContentProvider => SiteConfiguration.ContentProvider;

        public static PageModel GetPageModel(int publicationId, string urlPath, bool addIncludes = true)       
            => ContentProvider.GetPageModel(urlPath, CreateDocsLocalization(publicationId), addIncludes);

        public static PageModel GetPageModel(int publicationId, int pageId, bool addIncludes = true)
            => ContentProvider.GetPageModel(pageId, CreateDocsLocalization(publicationId), addIncludes);

        public static EntityModel GetEntityModel(int publicationId, string id)
           => ContentProvider.GetEntityModel(id, CreateDocsLocalization(publicationId));

        public static EntityModel GetEntityModel(int publicationId, int id)
            => ContentProvider.GetEntityModel($"{id}-{id}", CreateDocsLocalization(publicationId));

        public static StaticContentItem GetStaticContentItem(int publicationId, string urlPath)
            => ContentProvider.GetStaticContentItem(urlPath, CreateDocsLocalization(publicationId));

        public static StaticContentItem GetStaticContentItem(int publicationId, int binaryId)
            => ContentProvider.GetStaticContentItem(binaryId, CreateDocsLocalization(publicationId));

        public static void PopulateDynamicList(int publicationId, DynamicList dynamicList)
            => ContentProvider.PopulateDynamicList(dynamicList, CreateDocsLocalization(publicationId));
    }
}
