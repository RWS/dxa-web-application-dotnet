using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.UI;
using Sdl.Web.Common;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Html;

namespace Sdl.Web.Mvc.OutputCache
{
    /// <summary>
    /// DXAOutputCacheAttribute
    /// 
    /// Allows view rendering output caching using the Dxa caching mechanism. Any Entity Models that should not be cached
    /// on the page can be annotated with the [DxaNoOutputCache] attribute.
    /// </summary>
    public class DxaOutputCacheAttribute : ActionFilterAttribute
    {
        private const string DxaDisableOutputCache = "DxaDisableOutputCache";
        private HtmlTextWriter _responseWriter;
        private TextWriter _originalResponseWriter;
        private string _cacheKey;
        private readonly bool _enabled;
        private readonly bool _ignorePreview;

        public DxaOutputCacheAttribute()
        {
            string setting = WebConfigurationManager.AppSettings["output-caching-enabled"];
            _enabled = !string.IsNullOrEmpty(setting) && setting.Equals("true", StringComparison.InvariantCultureIgnoreCase);

            // used to override our check for being in a preview session. if this property is found in the
            // configuration we ignore preview sessions
            setting = WebConfigurationManager.AppSettings["output-caching-in-preview"];
            _ignorePreview = !string.IsNullOrEmpty(setting) && setting.Equals("true", StringComparison.InvariantCultureIgnoreCase);
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!_enabled) return;

            _originalResponseWriter = null;
            _responseWriter = new HtmlTextWriter(new StringWriter());                    
            _cacheKey = CalcCacheKey(filterContext);
            string cachedOutput;
            SiteConfiguration.CacheProvider.TryGet(_cacheKey, CacheRegions.RenderedOutput, out cachedOutput);            
            if (cachedOutput == null)
            {
                _originalResponseWriter = HttpContext.Current.Response.Output;
                HttpContext.Current.Response.Output = _responseWriter;
            }
            else
            {
                filterContext.Result = new ContentResult { Content = cachedOutput };
            }
        }

        private static ControllerBase GetTopLevelController(ResultExecutedContext filterContext)
        {
            ViewContext viewContext = filterContext.ParentActionViewContext;
            if (viewContext == null) return filterContext.Controller;
            while (viewContext.ParentActionViewContext != null)
            {
                viewContext = viewContext.ParentActionViewContext;
            }
            return viewContext.Controller;
        }   

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (!_enabled) return;
            bool commitCache = filterContext.IsChildAction || !filterContext.Controller.TempData.ContainsKey(DxaDisableOutputCache);
            EntityModel model = filterContext.Controller.ViewData.Model as EntityModel;
            if (filterContext.IsChildAction)
            {                 
                // since we are dealing with a child action it's likely we are working with an entity model. if so we should
                // check if the entity is marked for no output caching or returns volatile. in this case we tell our parent
                // controller which would be the page controller not to perform output caching at the page level and instead
                // we just switch over to entity level caching but not for this particular model.             
                if (model != null &&
                    (Attribute.GetCustomAttribute(model.GetType(), typeof (DxaNoOutputCacheAttribute)) != null || model.IsVolatile))
                {
                    var controller = GetTopLevelController(filterContext);
                    controller.TempData[DxaDisableOutputCache] = true;
                    commitCache = false;
                }
            }

            // we normally do not want view rendered output cached in preview but we can have the option to turn this off if set in the
            // web.config (for debug/testing purposes)            
            commitCache = (_ignorePreview || !WebRequestContext.IsPreview) && (Attribute.GetCustomAttribute(filterContext.Controller.GetType(), typeof(DxaNoOutputCacheAttribute)) == null) && commitCache;

            if (_originalResponseWriter != null)
            {
                HttpContext.Current.Response.Output = _originalResponseWriter;
                string html = ((StringWriter)_responseWriter.InnerWriter).ToString();
                filterContext.HttpContext.Response.Write(html);
                if (commitCache)
                {
                    // since our model is cached we need to make sure we decorate the markup with XPM markup
                    // as this is done outside the child action normally on a non-cached entity.
                    if (model != null && WebRequestContext.IsPreview)
                    {
                        html = Markup.TransformXpmMarkupAttributes(html);
                    }
                    html = Markup.DecorateMarkup(new MvcHtmlString(html), model).ToString();
                    // we finally have a fully rendered model's html that we can cache to our region
                    SiteConfiguration.CacheProvider.Store(_cacheKey, CacheRegions.RenderedOutput, html);
                }
            }
        }

        private static string CalcCacheKey(ActionExecutingContext filterContext)
        {            
            var sb = new StringBuilder();
            sb.Append(filterContext.ActionDescriptor.ControllerDescriptor.ControllerName);
            sb.Append(filterContext.ActionDescriptor.ActionName);
            foreach (var p in filterContext.ActionParameters.Where(p => p.Value != null))
            {
                sb.AppendFormat($"{p.Key.GetHashCode()}:{p.Value.GetHashCode()}");
            }
            return sb.ToString();
        }
    }
}
