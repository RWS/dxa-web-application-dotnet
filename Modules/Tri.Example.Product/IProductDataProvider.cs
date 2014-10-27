using System;

namespace Tri.Example.Products
{
    public interface IProductDataProvider
    {
        void FetchProductData(Product model);
    }
}
