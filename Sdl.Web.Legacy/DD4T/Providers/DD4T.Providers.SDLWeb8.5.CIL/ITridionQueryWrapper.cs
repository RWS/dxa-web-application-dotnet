using Tridion.ContentDelivery.DynamicContent.Query;
using DD4T.ContentModel.Querying;

namespace DD4T.Providers.SDLWeb85.CIL
{
    public interface ITridionQueryWrapper : IQuery
    {
        Query ToTridionQuery();
    }
}
