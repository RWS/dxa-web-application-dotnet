using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using Sdl.Web.Mvc.Configuration;
using Sdl.Web.Common.Configuration;
using SI4T.Query.Solr;
using SI4T.Query.Models;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Logging;
using System.Text.RegularExpressions;

namespace Sdl.Web.Modules.Search.Solr
{
    public class SolrProvider : ISearchProvider
    {
        public IContentResolver ContentResolver { get; set; }

        public virtual SearchQuery<T> ExecuteQuery<T>(NameValueCollection parameters, SearchQuery<T> data, string searchIndex)
        {
            NameValueCollection processedParameters = SetupParameters(parameters);
            processedParameters["rows"] = data.PageSize.ToString();
            Log.Debug("Connecting to search index: {0}", searchIndex);
            Connection conn = new Connection(searchIndex);
            foreach(var key in processedParameters.AllKeys)
            {
                Log.Debug("Parameter '{0}' is '{1}'", key, processedParameters[key]);
            }
            var results = conn.ExecuteQuery(processedParameters);
            Log.Debug("Search query {0} returned {1} results", results.QueryUrl, results.Total);
            if (results.HasError)
            {
                var ex = new Exception("Error executing search: " + results.ErrorDetail);
                Log.Error(ex);
                throw ex;
            }
            data.QueryText = parameters["q"];
            data.Start = results.Start;
            data.Total = results.Total;
            data.PageSize = results.PageSize;
            data.HasMore = results.Start + results.PageSize <= results.Total;
            data.CurrentPage = results.PageSize == 0 ? 1 : results.Start / results.PageSize + 1;
            data.Parameters = processedParameters;
            foreach (var result in results.Items)
            {
                data.Results.Add(MapResult<T>(result));
            }
            return data;
        }

        protected virtual T MapResult<T>(SearchResult result)
        {
            //TODO - utilize some kind of semantic mapping for returning the result
            if (typeof(T) == typeof(Teaser))
            {
                var teaser = new Teaser();
                var url = ContentResolver != null ? ContentResolver.ResolveLink(result.Url) : result.Url;
                teaser.Headline = result.Title;
                teaser.Link = new Link { Url = url };
                teaser.Text = result.Summary;
                return (T)Convert.ChangeType(teaser, typeof(T));
            }
            else if (typeof(T) == typeof(SearchResult))
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            throw new Exception("Currently only Sdl.Web.Common.Models.Teaser and SI4T.Query.Models.SearchResult are supported model generic types");
        }

        protected virtual NameValueCollection SetupParameters(NameValueCollection parameters)
        {
            var result = new NameValueCollection();
            result.Add("fq", "publicationid:" + WebRequestContext.Localization.LocalizationId);
            foreach (var key in parameters.AllKeys)
            {
                string value = "";
                switch(key)
                {
                    case "q":
                        value = PrepareQuery(parameters[key]);
                        break;
                    default:
                        value = parameters[key];
                        break;
                }
                result.Add(key, value);
            }
            if (!result.AllKeys.Contains("start"))
            {
                result["start"] = "1";
            }
            result.Add("hl", "true");
            return result;
        }

        private string PrepareQuery(string query)
        {
            var escapedQuery = Regex.Replace(query, @"([\\&|+\-!(){}[\]^\""~*?:])", delegate(Match match)
            {
                return @"\" + match.Groups[1].Value;
            });
            return escapedQuery; 
        }
    }
}