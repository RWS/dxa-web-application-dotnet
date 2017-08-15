using System;
using DD4T.ContentModel.Contracts.Providers;
namespace DD4T.ContentModel.Factories
{
    public interface ITaxonomyFactory
    {
        ITaxonomyProvider TaxonomyProvider { get; set; }
        bool TryGetKeyword(string categoryUriToLookIn, string keywordName, out IKeyword keyword);
        IKeyword GetKeyword(string categoryUriToLookIn, string keywordName);
    }
}
