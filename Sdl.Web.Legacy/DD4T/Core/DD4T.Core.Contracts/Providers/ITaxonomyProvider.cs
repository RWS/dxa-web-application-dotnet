using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel.Contracts.Providers
{
    public interface ITaxonomyProvider : IProvider
    {
        IKeyword GetKeyword(string categoryUriToLookIn, string keywordName);
    }
}
