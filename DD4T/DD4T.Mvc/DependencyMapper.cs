using DD4T.Core.Contracts.DependencyInjection;
using DD4T.Mvc.Html;
using DD4T.Mvc.ViewModels.XPM;
using System;
using System.Collections.Generic;

namespace DD4T.Mvc
{
    public class DependencyMappings : IDependencyMapper
    {
        private IDictionary<Type, Type> Mappings()
        {
            var mappings = new Dictionary<Type, Type>();

            mappings.Add(typeof(IComponentPresentationRenderer), typeof(DefaultComponentPresentationRenderer));
            mappings.Add(typeof(IXpmMarkupService), typeof(XpmMarkupService));

            return mappings;
        }

        public IDictionary<Type, Type> SingleInstanceMappings
        {
            get
            {
                return null;
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
                return Mappings();
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