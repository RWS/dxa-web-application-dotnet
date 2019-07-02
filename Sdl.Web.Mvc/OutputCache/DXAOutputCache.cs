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
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Mvc.Html;

namespace Sdl.Web.Mvc.OutputCache
{
    /// <summary>
    /// Allows view rendering output caching using the Dxa caching mechanism. Any Entity Models that should not be cached
    /// on the page can be annotated with the [DxaNoOutputCache] attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DxaOutputCacheAttribute : ActionFilterAttribute
    {
        [Serializable]
        private sealed class OutputCacheItem
        {
            public string ContentType { get; set; }
            public Encoding ContentEncoding { get; set; }
            public string Content { get; set; }
        }

        private static readonly object CacheKeyStack = new object();        
        private static readonly object DisablePageOutputCacheKey = new object();
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
            if(ctx.Controller.ViewData[DxaViewDataItems.DisableOutputCache] != null && (bool)ctx.Controller.ViewData[DxaViewDataItems.DisableOutputCache]) return;
            if (IgnoreCaching(ctx.Controller))
            {
                return;
            }

            OutputCacheItem cachedOutput = null;
            string cacheKey = CalcCacheKey(ctx);
            PushCacheKey(ctx, cacheKey);
            SiteConfiguration.CacheProvider.TryGet(cacheKey, CacheRegions.RenderedOutput, out cachedOutput);
            if (cachedOutput != null)
            {
                ctx.Result = new ContentResult
                {
                    Content = cachedOutput.Content,
                    ContentType = cachedOutput.ContentType,
                    ContentEncoding = cachedOutput.ContentEncoding
                };
            }
        }      

        public override void OnResultExecuting(ResultExecutingContext ctx)
        {
            if (!_enabled) return;
            if(ctx.Result is ContentResult) return;
            
            if (IgnoreCaching(ctx.Controller))
            {
                SetDisablePageOutputCache(ctx, true);
            }
            string cacheKey = PopCacheKey(ctx);
            if (cacheKey == null) return;
            OutputCacheItem cachedOutput;
            SiteConfiguration.CacheProvider.TryGet(cacheKey, CacheRegions.RenderedOutput, out cachedOutput);
            if (cachedOutput == null)
            {
                StringWriter cachingWriter = new StringWriter((IFormatProvider) CultureInfo.InvariantCulture);
                TextWriter originalWriter = ctx.HttpContext.Response.Output;
                ViewModel model = ctx.Controller.ViewData.Model as ViewModel;
                ctx.HttpContext.Response.Output = cachingWriter;
                SetCallback(ctx, (viewModel, commitCache) =>
                {
                    ctx.HttpContext.Response.Output = originalWriter;
                    string html = cachingWriter.ToString();
                    ctx.HttpContext.Response.Write(html);
                    if (!commitCache) return;
                    // since our model is cached we need to make sure we decorate the markup with XPM markup
                    // as this is done outside the child action normally on a non-cached entity.
                    // n.b. we should only do this if our text/html content
                    if (ctx.HttpContext.Response.ContentType.Equals("text/html"))
                    {
                        if (model != null && WebRequestContext.Localization.IsXpmEnabled)
                        {
                            html = Markup.TransformXpmMarkupAttributes(html);
                        }
                        html = Markup.DecorateMarkup(new MvcHtmlString(html), model).ToString();
                    }
                    OutputCacheItem cacheItem = new OutputCacheItem
                    {
                        Content = html,
                        ContentType = ctx.HttpContext.Response.ContentType,
                        ContentEncoding = ctx.HttpContext.Response.ContentEncoding
                    };
                    // we finally have a fully rendered model's html that we can cache to our region              
                    SiteConfiguration.CacheProvider.Store(cacheKey, CacheRegions.RenderedOutput, cacheItem);
                    if(viewModel!=null) Log.Trace($"ViewModel={viewModel.MvcData} added to DxaOutputCache.");
                });
            }
        }

        public override void OnResultExecuted(ResultExecutedContext ctx)
        {
            if (!_enabled) return;
            if (ctx.Result is ContentResult) return;

            if (IgnoreCaching(ctx.Controller))
            {
                //var controller = GetTopLevelController(ctx);
                //controller.TempData[DxaDisableOutputCache] = true;
                SetDisablePageOutputCache(ctx, true);
            }
            if (ctx.Exception != null)
            {
                RemoveCallback(ctx);
                return;
            }

            bool commitCache = ctx.IsChildAction || !DisablePageOutputCache(ctx);
            
            ViewModel model = ctx.Controller.ViewData.Model as ViewModel;
            if (ctx.IsChildAction)
            {
                // since we are dealing with a child action it's likely we are working with an entity model. if so we should
                // check if the entity is marked for no output caching or returns volatile. in this case we tell our parent
                // controller which would be the page controller not to perform output caching at the page level and instead
                // we just switch over to entity level caching but not for this particular model.             
                if (model != null && (IgnoreCaching(model) || model.IsVolatile))
                {
                    SetDisablePageOutputCache(ctx, true);
                    commitCache = false;
                    Log.Trace($"ViewModel={model.MvcData} is marked not to be added to DxaOutputCache.");
                }
            }

            // we normally do not want view rendered output cached in preview but we can have the option to turn this off if set in the
            // web.config (for debug/testing purposes)            
            commitCache = (_ignorePreview || !WebRequestContext.IsSessionPreview) && (!IgnoreCaching(ctx.Controller)) && commitCache;
            Action<ViewModel,bool> callback = GetCallback(ctx);
            if (callback == null) return;
            RemoveCallback(ctx);
            callback(model, commitCache);
        }      

        private static string CalcCacheKey(ActionExecutingContext ctx)
        {
            var sb = new StringBuilder();
            sb.Append($"{ctx.ActionDescriptor.UniqueId}-{ctx.HttpContext.Request.Url}-{ctx.HttpContext.Request.UserAgent}:{WebRequestContext.CacheKeySalt}");
            foreach (var p in ctx.ActionParameters.Where(p => p.Value != null))
            {
                sb.Append($"{p.Key.GetHashCode()}:{p.Value.GetHashCode()}-");
            }
            return sb.ToString();
        }

        private static bool DisablePageOutputCache(ControllerContext ctx)
        {
            bool result = false;
            if (ctx.HttpContext.Items.Contains(DisablePageOutputCacheKey))
            {
                result = (bool)ctx.HttpContext.Items[DisablePageOutputCacheKey];
            }
            return result;
        }

        private static void SetDisablePageOutputCache(ControllerContext ctx, bool disable)
        {
            ctx.HttpContext.Items[DisablePageOutputCacheKey] = disable;
        }

        private static string GetKey(ControllerContext ctx)
        {
            string key = "__dxa__";
            if (ctx.IsChildAction) key += "c";
            ViewModel model = ctx.Controller.ViewData.Model as ViewModel;
            if (model == null) return key;
            key += model.GetHashCode();
            return key;
        }

        private static object GetCallbackKeyObject(ControllerContext ctx) => GetKey(ctx) + "__cb_";

        private static void RemoveCallback(ControllerContext ctx) => ctx.HttpContext.Items.Remove(GetCallbackKeyObject(ctx));

        private static Action<ViewModel,bool> GetCallback(ControllerContext ctx) => ctx.HttpContext.Items[GetCallbackKeyObject(ctx)] as Action<ViewModel,bool>;

        private static void SetCallback(ControllerContext ctx, Action<ViewModel,bool> callback) => ctx.HttpContext.Items[GetCallbackKeyObject(ctx)] = (object)callback;

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
            if (stack == null || stack.Count == 0) return null;
            return stack?.Pop();
        }

        private static bool IgnoreCaching(object obj) => Attribute.GetCustomAttribute(obj.GetType(), typeof (DxaNoOutputCacheAttribute)) != null;
    }
}
