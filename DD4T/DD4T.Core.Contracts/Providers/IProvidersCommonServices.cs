using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel.Contracts.Resolvers;
using DD4T.ContentModel.Contracts.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.ContentModel.Contracts.Providers
{
    public interface IProvidersCommonServices
    {
        IPublicationResolver PublicationResolver { get; }
        ILogger Logger { get; }
        IDD4TConfiguration Configuration { get; }
    }
}
