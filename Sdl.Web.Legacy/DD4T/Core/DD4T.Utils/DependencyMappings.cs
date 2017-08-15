using DD4T.ContentModel.Contracts.Caching;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Logging;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.Core.Contracts.DependencyInjection;
using DD4T.Core.Contracts.Resolvers;
using DD4T.Utils.Caching;
using DD4T.Utils.Logging;
using DD4T.Utils.Resolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Utils
{
    public class DependencyMappings : IDependencyMapper
    {
        private IDictionary<Type, Type> MappingsForSingleInstance()
        {
            var mappings = new Dictionary<Type, Type>();

            mappings.Add(typeof(ILinkResolver), typeof(DefaultLinkResolver));
            mappings.Add(typeof(IRichTextResolver), typeof(DefaultRichTextResolver));
            mappings.Add(typeof(IPublicationResolver), typeof(DefaultPublicationResolver));
            mappings.Add(typeof(IDD4TConfiguration), typeof(DD4TConfiguration));
            mappings.Add(typeof(ICacheAgent), typeof(DefaultCacheAgent));
            mappings.Add(typeof(ILogger), typeof(NullLogger));

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
