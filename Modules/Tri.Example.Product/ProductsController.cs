using Sdl.Web.Common.Interfaces;
using System;

namespace Tri.Example.Products
{
    public class ProductsController : Sdl.Web.Mvc.Controllers.BaseController
    {
        public IProductDataProvider ProductDataProvider { get; set; }
        public ProductsController(IContentProvider contentProvider, IRenderer renderer, IProductDataProvider productDataProvider)
        {
            ProductDataProvider = productDataProvider;
            ContentProvider = contentProvider;
            Renderer = renderer;
        }

        protected override object ProcessModel(object sourceModel, Type type)
        {
            var model = base.ProcessModel(sourceModel, type);
            if (model is Product)
            {
                ProductDataProvider.FetchProductData((Product)model);
            }
            return model;
        }
    }
}