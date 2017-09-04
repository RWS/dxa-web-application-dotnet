using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;

namespace DD4T.DI.Autofac
{
    public static class Mvc
    {
        public static void RegisterMvc(this ContainerBuilder builder)
        {
            var location = string.Format(@"{0}\bin\", AppDomain.CurrentDomain.BaseDirectory);
            var file = Directory.GetFiles(location, "DD4T.MVC.dll").FirstOrDefault();
            if (file == null)
                return;

            Assembly.LoadFile(file);
            var provider = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName.StartsWith("DD4T.MVC", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (provider == null)
                return;

            var providerTypes = provider.GetTypes();

            var iComponentPresentationRenderer = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.Html.IComponentPresentationRenderer", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            var defaultComponentPresentationRenderer = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.Html.DefaultComponentPresentationRenderer", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            var iXpmMarkupService = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.ViewModels.XPM.IXpmMarkupService", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            var defaultXpmMarkupService = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.ViewModels.XPM.XpmMarkupService", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            //register default ComponentPresentationRenderer
            if (iComponentPresentationRenderer != null || defaultComponentPresentationRenderer != null)
            {
                builder.RegisterType(defaultComponentPresentationRenderer).As(iComponentPresentationRenderer).PreserveExistingDefaults();
            }

            //register default XPmMarkupService
            if (iXpmMarkupService != null || defaultXpmMarkupService != null)
            {
                builder.RegisterType(defaultXpmMarkupService).As(iXpmMarkupService).PreserveExistingDefaults();
            }
        }
    }
}
