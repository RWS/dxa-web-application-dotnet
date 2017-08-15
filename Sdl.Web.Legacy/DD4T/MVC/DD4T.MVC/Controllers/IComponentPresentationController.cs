using DD4T.ContentModel.Factories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace DD4T.Mvc.Controllers
{
    public interface IComponentPresentationController : IController
    {
        IComponentPresentationFactory ComponentPresentationFactory { get; set; }
    }
}
