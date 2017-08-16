using Autofac;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels;
using DD4T.ViewModels.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Autofac
{
    public static class ViewModels
    {
        public static void RegisterViewModels(this ContainerBuilder builder)
        {
            //viewmodels
            builder.RegisterType<ReflectionOptimizer>()
                 .As<IReflectionHelper>()
                 .SingleInstance()
                 .PreserveExistingDefaults();

            builder.RegisterType<DefaultViewModelResolver>()
               .As<IViewModelResolver>()
               .SingleInstance()
               .PreserveExistingDefaults();

            builder.RegisterType<ViewModelFactory>()
               .As<IViewModelFactory>()
               .SingleInstance()
               .PreserveExistingDefaults();

            builder.RegisterType<WebConfigViewModelKeyProvider>()
             .As<IViewModelKeyProvider>()
             .SingleInstance()
             .PreserveExistingDefaults();
        }
    }
}
