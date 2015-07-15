using System;
using System.IO;
using System.Web.Mvc;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;

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
                AreaName + "_Default",
                "{controller}/{action}/{id}",
                new { controller = "Entity", action = "Entity", id = UrlParameter.Optional }
            );
            RegisterAllViewModels();
        }

        /// <summary>
        /// Registers a View Model without associated View.
        /// </summary>
        /// <param name="modelType">The View Model type.</param>
        /// <remarks>
        /// </remarks>
        protected void RegisterViewModel(Type modelType)
        {
            ModelTypeRegistry.RegisterViewModel(null, modelType);
        }

        /// <summary>
        /// Registers a View Model and associated View.
        /// </summary>
        /// <param name="viewName">The name of the View.</param>
        /// <param name="modelType">The view model type</param>
        /// <param name="controllerName">The controller name (eg Search)</param>
        protected void RegisterViewModel(string viewName, Type modelType, string controllerName = "Entity")
        {
            MvcData mvcData = new MvcData { AreaName = AreaName, ControllerName = controllerName, ViewName = viewName };
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
            DateTime timer = DateTime.Now;
            int viewCount = 0;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string path = Path.Combine(baseDir, "Areas", this.AreaName, "Views");
            Log.Debug("Staring to register views for area: {0}", this.AreaName);
            foreach (string file in Directory.GetFiles(path, "*.cshtml", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(path.Length + 1);
                string virtualPath = @"~/" + file.Replace(baseDir, string.Empty).Replace("\\", "/");
                int pos = relativePath.IndexOf("\\");
                if (pos > 0)
                {
                    string controller = relativePath.Substring(0, pos);
                    string view = relativePath.Substring(pos + 1);
                    int extnPos = view.LastIndexOf(".cshtml");
                    view = view.Substring(0, extnPos);
                    MvcData mvcData = new MvcData { AreaName = this.AreaName, ControllerName = controller, ViewName = view };
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
