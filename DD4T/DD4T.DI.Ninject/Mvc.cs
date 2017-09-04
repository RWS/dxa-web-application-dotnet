//using Ninject;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using System.Threading.Tasks;

//namespace DD4T.DI.Ninject
//{
//    public static class Mvc
//    {
//        public static void BindMvc(this IKernel kernel)
//        {
//            var binDirectory = string.Format(@"{0}\bin\", AppDomain.CurrentDomain.BaseDirectory);
//            if (!Directory.Exists(binDirectory))
//                return;

//            var file = Directory.GetFiles(binDirectory, "DD4T.MVC.dll").FirstOrDefault();
//            if (file == null)
//                return;

//            var load = Assembly.LoadFile(file);
//            var provider = AppDomain.CurrentDomain.GetAssemblies().Where(ass => ass.FullName.StartsWith("DD4T.MVC")).FirstOrDefault();
//            if (provider == null)
//                return;

//            var providerTypes = provider.GetTypes();

//            var iComponentPresentationRenderer = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.Html.IComponentPresentationRenderer")).FirstOrDefault();
//            var defaultComponentPresentationRenderer = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.Html.DefaultComponentPresentationRenderer")).FirstOrDefault();

//            var iXpmMarkupService = providerTypes.Where(a => a.FullName.Equals("DD4T.MVC.ViewModels.XPM.IXpmMarkupService")).FirstOrDefault();
//            var defaultXpmMarkupService = providerTypes.Where(a => a.FullName.Equals("DD4T.Mvc.ViewModels.XPM.XpmMarkupService")).FirstOrDefault();

//            if (iComponentPresentationRenderer != null || defaultComponentPresentationRenderer != null)
//            {
//                if (kernel.TryGet(iComponentPresentationRenderer) == null)
//                    kernel.Bind(iComponentPresentationRenderer).To(defaultComponentPresentationRenderer);
//            }

//            if (iXpmMarkupService != null || defaultXpmMarkupService != null)
//            {
//                if (kernel.TryGet(iXpmMarkupService) == null)
//                    kernel.Bind(iXpmMarkupService).To(defaultXpmMarkupService);
//            }
//        }
//    }
//}