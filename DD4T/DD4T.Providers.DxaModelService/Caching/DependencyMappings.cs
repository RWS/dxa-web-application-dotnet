using DD4T.ContentModel.Contracts.Caching;
using DD4T.Core.Contracts.DependencyInjection;
using System;
using System.Collections.Generic;

namespace DD4T.Providers.DxaModelService.Caching
{
    public class DependencyMappings : IDependencyMapper
    {
        private IDictionary<Type, Type> GetMappings() => new Dictionary<Type, Type> {{typeof (ICacheAgent), typeof (DxaCacheAgent)}};

        public IDictionary<Type, Type> SingleInstanceMappings => GetMappings();

        public IDictionary<Type, Type> PerHttpRequestMappings => null;

        public IDictionary<Type, Type> PerLifeTimeMappings => null;

        public IDictionary<Type, Type> PerDependencyMappings => null;
    }
}
