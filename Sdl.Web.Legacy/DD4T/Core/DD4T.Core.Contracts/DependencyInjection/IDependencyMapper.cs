using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Core.Contracts.DependencyInjection
{
    /// <summary>
    /// Used by DD4T.DI Packages to register default type's 
    /// </summary>
    public interface IDependencyMapper
    {
        IDictionary<Type, Type> SingleInstanceMappings  { get; }
        IDictionary<Type, Type> PerHttpRequestMappings { get; }
        IDictionary<Type, Type> PerLifeTimeMappings { get; }
        IDictionary<Type, Type> PerDependencyMappings { get; }
    }
}
