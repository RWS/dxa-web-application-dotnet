using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels;
using DD4T.ViewModels.Reflection;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Unity
{
    public static class ViewModels
    {
        public static void RegisterViewModels(this IUnityContainer container)
        {
            if (!container.IsRegistered<IReflectionHelper>())
                container.RegisterType<IReflectionHelper, ReflectionOptimizer>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<IViewModelResolver>())
                container.RegisterType<IViewModelResolver, DefaultViewModelResolver>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<IViewModelFactory>())
                container.RegisterType<IViewModelFactory, ViewModelFactory>(new ContainerControlledLifetimeManager());

            if (!container.IsRegistered<IViewModelKeyProvider>())
                container.RegisterType<IViewModelKeyProvider, WebConfigViewModelKeyProvider>(new ContainerControlledLifetimeManager());

        }
    }
}
