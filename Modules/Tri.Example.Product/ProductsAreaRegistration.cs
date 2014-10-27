using System.Web.Mvc;

namespace Tri.Example.Products
{
    public class ProductsAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "Products";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                "Default_Products",
                "{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}