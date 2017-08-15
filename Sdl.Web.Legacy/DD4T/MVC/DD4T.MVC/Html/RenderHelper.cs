using DD4T.Core.Contracts.ViewModels;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace DD4T.Mvc.Html
{
    public static class RenderHelper
    {
        public static MvcHtmlString Render(this HtmlHelper htmlHelper, IRenderableViewModel viewModel)
        {
            return htmlHelper.Action(viewModel.RenderData.Action, viewModel.RenderData.Controller, new { model = viewModel, view = viewModel.RenderData.View });
        }
    }
}