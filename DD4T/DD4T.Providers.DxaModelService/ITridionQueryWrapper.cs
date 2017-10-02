using Tridion.ContentDelivery.DynamicContent.Query;
using DD4T.ContentModel.Querying;

namespace DD4T.Providers.DxaModelService
{
    public interface ITridionQueryWrapper : IQuery
    {
        Query ToTridionQuery();
    }
}
