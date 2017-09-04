using DD4T.ContentModel.Contracts.Logging;
using DD4T.Core.Contracts.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Logging.Log4net
{
    public class DependencyMappings : IDependencyMapper
    {
        private IDictionary<Type, Type> GetSingleInstanceMappings()
        {
            var mappings = new Dictionary<Type, Type>();

            mappings.Add(typeof(ILogger), typeof(DefaultLogger));

            return mappings;
        }

        public IDictionary<Type, Type> SingleInstanceMappings
        {
            get
            {
                return GetSingleInstanceMappings();
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

