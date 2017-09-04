using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels;
using DD4T.ViewModels.Reflection;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Ninject
{
    public static class ViewModels
    {
        public static void BindViewModels(this IKernel kernel)
        {
            if (kernel.TryGet<IReflectionHelper>() == null)
                kernel.Bind<IReflectionHelper>().To<ReflectionOptimizer>().InSingletonScope();

            if (kernel.TryGet<IViewModelResolver>() == null)
                kernel.Bind<IViewModelResolver>().To<DefaultViewModelResolver>().InSingletonScope();

            if (kernel.TryGet<IViewModelFactory>() == null)
                kernel.Bind<IViewModelFactory>().To<ViewModelFactory>().InSingletonScope();

            if (kernel.TryGet<IViewModelKeyProvider>() == null)
                kernel.Bind<IViewModelKeyProvider>().To<WebConfigViewModelKeyProvider>().InSingletonScope();
        }
    }
}
