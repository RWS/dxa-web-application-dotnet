using DD4T.ContentModel.Contracts.Caching;
using DD4T.Core.Contracts.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Caching.ApacheMQ
{
    public class DependencyMappings : IDependencyMapper
    {
        private IDictionary<Type, Type> GetMappings()
        {
            var mappings = new Dictionary<Type, Type>();

            mappings.Add(typeof(IMessageProvider), typeof(JMSMessageProvider));

            return mappings;
        }
        
        public IDictionary<Type, Type> SingleInstanceMappings
        {
            get
            {
                return GetMappings();
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
