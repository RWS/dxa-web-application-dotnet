using DD4T.ContentModel.Factories;
using DD4T.Core.Contracts.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Factories
{
    public class DependencyMappings : IDependencyMapper
    {

        private IDictionary<Type, Type> MappingsForPerLifeTimeScope()
        {
            var mappings = new Dictionary<Type, Type>();

            mappings.Add(typeof(IFactoryCommonServices), typeof(FactoryCommonServices));
            mappings.Add(typeof(IPageFactory), typeof(PageFactory));
            mappings.Add(typeof(IComponentPresentationFactory), typeof(ComponentPresentationFactory));
            mappings.Add(typeof(IComponentFactory), typeof(ComponentFactory));
            mappings.Add(typeof(IBinaryFactory), typeof(BinaryFactory));
            mappings.Add(typeof(ILinkFactory), typeof(LinkFactory));

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
                return this.MappingsForPerLifeTimeScope();
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
