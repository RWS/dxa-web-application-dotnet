using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Core.Contracts.DependencyInjection;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ViewModels
{
    public class DependencyMappings : IDependencyMapper
    {
        private IDictionary<Type, Type> MappingsForSingleInstance()
        {
            var mappings = new Dictionary<Type, Type>();

            mappings.Add(typeof(IContextResolver), typeof(DefaultContextResolver));
            mappings.Add(typeof(IReflectionHelper), typeof(ReflectionOptimizer));
            mappings.Add(typeof(IViewModelResolver), typeof(DefaultViewModelResolver));
            mappings.Add(typeof(IViewModelFactory), typeof(ViewModelFactory));
            mappings.Add(typeof(IViewModelKeyProvider), typeof(WebConfigViewModelKeyProvider));

            return mappings;
        }
        public IDictionary<Type, Type> SingleInstanceMappings
        {
            get
            {
                return this.MappingsForSingleInstance();
            }
        }

        public IDictionary<Type, Type> PerHttpRequestMappings
        {
            get
            {
                return null;
            }
        }

        public IDictionary<Type, Type> PerLifeTimeMappings
        {
            get
            {
                return null;
            }
        }

        public IDictionary<Type, Type> PerDependencyMappings
        {
            get
            {
                return null;
            }
        }
    }
}
