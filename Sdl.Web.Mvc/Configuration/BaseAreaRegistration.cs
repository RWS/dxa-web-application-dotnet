using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Configuration
{
    /// <summary>
    /// Base AreaRegistration class which helper methods to register views models for the area with the application
    /// </summary>
    public abstract class BaseAreaRegistration : AreaRegistration
    {
        public override void RegisterArea(AreaRegistrationContext context)
        {
            //Default Route - required for sub actions (region/entity/navigation etc.)
            context.MapRoute(
                this.AreaName + "_Default",
                "{controller}/{action}/{id}",
                new { controller = "Entity", action = "Entity", id = UrlParameter.Optional }
            );
            RegisterAllViewModels();
        }

        /// <summary>
        /// Explicitly register a view model
        /// </summary>
        /// <param name="viewName">The name of the view (eg SearchResults)</param>
        /// <param name="modelType">The view model type</param>
        /// <param name="controllerName">The controller name (eg Search)</param>
        protected virtual void RegisterViewModel(string viewName, Type modelType, string controllerName = "Entity")
        {
            var mvcData = new MvcData { AreaName = AreaName, ControllerName = controllerName, ViewName = viewName };
            ModelTypeRegistry.RegisterViewModel(mvcData, modelType);
        }

        /// <summary>
        /// Automatically register all view models for an area. This is done by searching the file system
        /// for all .cshtml files, determining the controller and view names from the path, and using the
        /// BuildManager to determine the model type by compiling the view. Note that if your area contains
        /// a lot of views, this can be a lengthy process and you might be better off explicitly registering
        /// your views with the RegisterViewModel method
        /// </summary>
        protected virtual void RegisterAllViewModels()
        {
            var timer = DateTime.Now;
            int viewCount = 0;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, "Areas", this.AreaName, "Views");
            Log.Debug("Staring to register views for area: {0}", this.AreaName);
            foreach (var file in Directory.GetFiles(path, "*.cshtml", SearchOption.AllDirectories))
            {
                var relativePath = file.Substring(path.Length + 1);
                var virtualPath = @"~/" + file.Replace(baseDir, string.Empty).Replace("\\", "/");
                var pos = relativePath.IndexOf("\\");
                if (pos > 0)
                {
                    var controller = relativePath.Substring(0, pos);
                    var view = relativePath.Substring(pos + 1);
                    var extnPos = view.LastIndexOf(".cshtml");
                    view = view.Substring(0, extnPos);
                    var mvcData = new MvcData { AreaName = this.AreaName, ControllerName = controller, ViewName = view };
                    try
                    {
                        ModelTypeRegistry.RegisterViewModel(mvcData, virtualPath);
                        viewCount++;
                    }
                    catch
                    {
                        //Do nothing - we ignore views that are not strongly typed
                    }
                }
                else
                {
                    Log.Warn("Cannot add view {0} to view model registry as it is not in a {ControllerName} subfolder", file);
                }
            }
            Log.Info("Registered {0} views for area {1} in {2} milliseconds. This startup overhead can be reduced by explicitly registering view using the Sdl.Web.Mvc.Configuration.BaseAreaRegistration.RegisterView() method.", viewCount, this.AreaName, (DateTime.Now - timer).TotalMilliseconds);
        }
    }
}
