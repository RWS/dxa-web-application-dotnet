using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Web.Mvc;
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
        private static readonly object CacheKeyStack = new object();
        private const string DxaDisableOutputCache = "DxaDisableOutputCache";
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
       
        public override void OnActionExecuting(ActionExecutingContext ctx)
        {
            if (!_enabled) return;
            string cachedOutput;
            string cacheKey = CalcCacheKey(ctx);
            PushCacheKey(ctx, cacheKey);
            SiteConfiguration.CacheProvider.TryGet(cacheKey, CacheRegions.RenderedOutput, out cachedOutput);
            if (cachedOutput != null)
            {
                ctx.Result = new ContentResult { Content = cachedOutput };
            }
        }      

        public override void OnResultExecuting(ResultExecutingContext ctx)
        {
            if (!_enabled) return;
            StringWriter cachingWriter = new StringWriter((IFormatProvider)CultureInfo.InvariantCulture);
            TextWriter originalWriter = ctx.HttpContext.Response.Output;
            ctx.HttpContext.Response.Output = cachingWriter;
            SetCallback(ctx, (model, commitCache) =>
            {
                ctx.HttpContext.Response.Output = originalWriter;
                string html = cachingWriter.ToString();
                ctx.HttpContext.Response.Write(html);
                if (!commitCache) return;                
                // since our model is cached we need to make sure we decorate the markup with XPM markup
                // as this is done outside the child action normally on a non-cached entity.
                if (model != null && WebRequestContext.IsPreview)
                {
                    html = Markup.TransformXpmMarkupAttributes(html);
                }
                html = Markup.DecorateMarkup(new MvcHtmlString(html), model).ToString();
                // we finally have a fully rendered model's html that we can cache to our region
                string cacheKey = PopCacheKey(ctx);
                SiteConfiguration.CacheProvider.Store(cacheKey, CacheRegions.RenderedOutput, html);
            });
        }

        public override void OnResultExecuted(ResultExecutedContext ctx)
        {
            if (!_enabled) return;
            if (ctx.Exception != null)
            {
                RemoveCallback(ctx);
                return;
            }

            bool commitCache = ctx.IsChildAction ||
                               !ctx.Controller.TempData.ContainsKey(DxaDisableOutputCache);
            EntityModel model = ctx.Controller.ViewData.Model as EntityModel;
            if (ctx.IsChildAction)
            {
                // since we are dealing with a child action it's likely we are working with an entity model. if so we should
                // check if the entity is marked for no output caching or returns volatile. in this case we tell our parent
                // controller which would be the page controller not to perform output caching at the page level and instead
                // we just switch over to entity level caching but not for this particular model.             
                if (model != null &&
                    (Attribute.GetCustomAttribute(model.GetType(), typeof(DxaNoOutputCacheAttribute)) != null ||
                     model.IsVolatile))
                {
                    var controller = GetTopLevelController(ctx);
                    controller.TempData[DxaDisableOutputCache] = true;
                    commitCache = false;
                }
            }

            // we normally do not want view rendered output cached in preview but we can have the option to turn this off if set in the
            // web.config (for debug/testing purposes)            
            commitCache = (_ignorePreview || !WebRequestContext.IsPreview) &&
                          (Attribute.GetCustomAttribute(ctx.Controller.GetType(),
                              typeof(DxaNoOutputCacheAttribute)) == null) && commitCache;
            Action<EntityModel, bool> callback = GetCallback(ctx);
            if (callback == null) return;
            RemoveCallback(ctx);
            callback(model, commitCache);
        }

        private static ControllerBase GetTopLevelController(ControllerContext filterContext)
        {
            ViewContext viewContext = filterContext.ParentActionViewContext;
            if (viewContext == null) return filterContext.Controller;
            while (viewContext.ParentActionViewContext != null)
            {
                viewContext = viewContext.ParentActionViewContext;
            }
            return viewContext.Controller;
        }

        private static string CalcCacheKey(ActionExecutingContext ctx)
        {
            var sb = new StringBuilder();
            sb.Append($"{ctx.ActionDescriptor.UniqueId}-{ctx.HttpContext.Request.QueryString}-");
            foreach (var p in ctx.ActionParameters.Where(p => p.Value != null))
            {
                sb.Append($"{p.Key.GetHashCode()}:{p.Value.GetHashCode()}-");
            }
            return sb.ToString();
        }

        private static string GetKey(ControllerContext ctx)
        {
            string key = "__dxa__";
            if (ctx.IsChildAction) key += "c";
            EntityModel model = ctx.Controller.ViewData.Model as EntityModel;
            if (model == null) return key;
            key += model.Id;
            return key;
        }

        private static object GetCallbackKeyObject(ControllerContext ctx) => GetKey(ctx) + "__cb_";

        private static void RemoveCallback(ControllerContext ctx) => ctx.HttpContext.Items.Remove(GetCallbackKeyObject(ctx));

        private static Action<EntityModel, bool> GetCallback(ControllerContext ctx) => ctx.HttpContext.Items[GetCallbackKeyObject(ctx)] as Action<EntityModel, bool>;

        private static void SetCallback(ControllerContext ctx, Action<EntityModel, bool> callback) => ctx.HttpContext.Items[GetCallbackKeyObject(ctx)] = (object)callback;

        private static void PushCacheKey(ControllerContext ctx, string key)
        {
            Stack<string> stack = ctx.HttpContext.Items[CacheKeyStack] as Stack<string>;
            if (stack == null)
            {
                stack = new Stack<string>();
                ctx.HttpContext.Items[CacheKeyStack] = stack;
            }
            stack.Push(key);
        }

        private static string PopCacheKey(ControllerContext ctx)
        {
            Stack<string> stack = ctx.HttpContext.Items[CacheKeyStack] as Stack<string>;
            return stack?.Pop();
        }
    }
}
