using System;

namespace Tri.Example.Products
{
    public class MyProductDataProvider : IProductDataProvider
    {
        public void FetchProductData(Product model)
        {
            model.Price = 99.99;
            model.Stock = 10;
        }
    }
}