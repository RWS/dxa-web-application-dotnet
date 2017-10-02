using System;
using System.Collections.Generic;
using System.Linq;
using Dynamic = DD4T.ContentModel;
using Tridion.ContentDelivery.Taxonomies;
using DD4T.ContentModel.Contracts.Providers;

namespace DD4T.Providers.SDLTridion2011sp1
{
    [Obsolete]
    public class TridionTaxonomyProvider : BaseProvider, ITaxonomyProvider, IDisposable
    {
        private TaxonomyFactory _taxonomyFactory;

        public TaxonomyFactory TaxonomyFactory
        {
            get
            {
                if (_taxonomyFactory == null)
                {
                    _taxonomyFactory = new TaxonomyFactory();
                }
                return _taxonomyFactory;
            }
        }

        public TridionTaxonomyProvider(IProvidersCommonServices commonServices)
            : base(commonServices)
        {
        }

        #region ITaxonomyProvider

        public Dynamic.IKeyword GetKeyword(string categoryUriToLookIn, string keywordName)
        {
            //Create filter to retrieve all keywords in a taxonomy
            CompositeFilter compFilter = new CompositeFilter();
            compFilter.DepthFiltering(DepthFilter.UnlimitedDepth, DepthFilter.FilterUp);
            compFilter.DepthFiltering(DepthFilter.UnlimitedDepth, DepthFilter.FilterDown);

            //Get keywords in taxonomy (hierarchically)
            IEnumerable<Keyword> taxonomy = null;
            try

            {
                //Ugly way to see if a taxonomy exists. Alternative is to loop through all taxonomys in Tridion and check if the categoryUriToLookIn exists...
                taxonomy = TaxonomyFactory.GetTaxonomyKeywords(categoryUriToLookIn, compFilter, new TaxonomyHierarchyFormatter()).KeywordChildren.Cast<Keyword>();
            }
            catch (Exception)
            {
                //TODO: Trace
                return null;
            }

            //Search in taxonomy
            Keyword foundKeyword = null;
            foreach (var currentKeyword in taxonomy)
            {
                string currentKeywordName = currentKeyword.KeywordName;
                if (!currentKeywordName.Equals(keywordName, StringComparison.InvariantCultureIgnoreCase))
                {
                    foundKeyword = recursive(currentKeyword.KeywordChildren.Cast<Keyword>().ToList(), keywordName);
                }
                else
                {
                    foundKeyword = currentKeyword;
                }
                if (foundKeyword != null)
                {
                    Dynamic.Keyword returnKeyword = new Dynamic.Keyword();

                    Keyword par = foundKeyword.ParentKeyword;
                    do
                    {
                        Dynamic.Keyword newParentKeyword = new Dynamic.Keyword();
                        newParentKeyword.Id = par.KeywordUri;
                        newParentKeyword.TaxonomyId = par.TaxonomyUri;
                        newParentKeyword.Title = par.KeywordName;
                        returnKeyword.ParentKeywords.Add(newParentKeyword); //Add the parentkeyword to the list
                        par = par.ParentKeyword;
                    } while (par != null);

                    //Add remaining properties to the returnKeyword
                    returnKeyword.Id = foundKeyword.KeywordUri;
                    returnKeyword.TaxonomyId = foundKeyword.TaxonomyUri;
                    returnKeyword.Title = foundKeyword.KeywordName;

                    return returnKeyword;
                }
            }

            return null;
        }

        #endregion ITaxonomyProvider

        #region IDisposable

        protected virtual void Dispose(bool isDisposed)
        {
            if (!isDisposed)
            {
                if (_taxonomyFactory != null)
                {
                    _taxonomyFactory.Dispose();
                    _taxonomyFactory = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

        private Keyword recursive(List<Keyword> keywords, string valueToLookFor)
        {
            Keyword returnValue = null;
            foreach (var item in keywords)
            {
                if (item.KeywordName.Equals(valueToLookFor, StringComparison.InvariantCultureIgnoreCase))
                {
                    returnValue = item;
                }
                else
                {
                    if (item.HasChildren)
                    {
                        returnValue = recursive(item.KeywordChildren.Cast<Keyword>().ToList(), valueToLookFor);
                    }
                }
                if (returnValue != null)
                {
                    break;
                }
            }
            return returnValue;
        }

        #endregion private
    }
}