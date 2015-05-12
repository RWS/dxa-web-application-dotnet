using System;
using Sdl.Web.Common.Models;

namespace Sdl.Web.Common.Interfaces
{
    public interface IContentProvider
    {
        IContentResolver ContentResolver { get; set; } // TODO TSI-788: should not be in this interface (see Java impl)

        //Get specific page/entity content
        PageModel GetPageModel(string url, bool addIncludes = true);
        string GetPageContent(string url); // TODO TSI-634: this may leak the underlying data representation (DD4T XML/JSON)
        EntityModel GetEntityModel(string id);
        string GetEntityContent(string url); // TODO TSI-634: this may leak the underlying data representation (DD4T XML/JSON)
        SitemapItem GetNavigationModel(string url);

        //Execute a query to get content
        ContentList<Teaser> PopulateDynamicList(ContentList<Teaser> list); // TODO TSI-634: too strongly typed (?)
    }
}