using System.Web.Mvc;
using DD4T.Mvc.Html;
using DD4T.ContentModel.Factories;

namespace DD4T.Mvc.Controllers
{
    public interface IPageController : IController
    {
        IPageFactory PageFactory { get; set; }
    }
}
