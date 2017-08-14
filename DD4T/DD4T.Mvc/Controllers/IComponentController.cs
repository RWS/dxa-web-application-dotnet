using System.Web.Mvc;
using DD4T.ContentModel.Factories;

namespace DD4T.Mvc.Controllers
{
    public interface IComponentController : IController
    {
        IComponentFactory ComponentFactory { get; set; }
    }
}
