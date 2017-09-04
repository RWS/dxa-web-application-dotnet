using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.DI.Autofac.Exceptions
{
    public class ProviderNotFoundException : Exception
    {
        public ProviderNotFoundException() : 
            base("DD4T provider not found. install one of the available Tridion Providers")
        {}
    }
}
