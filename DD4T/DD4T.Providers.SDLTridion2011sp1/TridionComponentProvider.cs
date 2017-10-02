using DD4T.ContentModel.Contracts.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Providers.SDLTridion2011sp1
{
    [Obsolete("use the ComponentPresentationProvider instead")]
    public class TridionComponentProvider : IComponentProvider
    {
        public string GetContent(string uri, string templateUri = "")
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastPublishedDate(string uri)
        {
            throw new NotImplementedException();
        }

        public List<string> GetContentMultiple(string[] componentUris)
        {
            throw new NotImplementedException();
        }

        public IList<string> FindComponents(ContentModel.Querying.IQuery queryParameters)
        {
            throw new NotImplementedException();
        }

        public int PublicationId
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
