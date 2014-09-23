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

namespace Sdl.Web.Modules.Search.Solr
{
    public class SolrProvider : ISearchProvider
    {
        public IContentResolver ContentResolver { get; set; }

        public SearchQuery<T> ExecuteQuery<T>(NameValueCollection parameters, SearchQuery<T> data)
        {
            NameValueCollection processedParameters = SetupParameters(parameters);
            processedParameters["rows"] = data.PageSize.ToString();
            Connection conn = new Connection(SiteConfiguration.GetConfig("search." + (SiteConfiguration.IsStaging ? "staging" : "live") + "IndexConfig"));
            var results = conn.ExecuteQuery(processedParameters);
            data.QueryText = parameters["q"];
            data.Start = results.Start;
            data.Total = results.Total;
            data.PageSize = results.PageSize;
            data.HasMore = results.Start + results.PageSize - 1 <= results.Total;
            data.CurrentPage = results.Start / results.PageSize + 1;//TODO check
            data.Parameters = processedParameters;
            foreach (var result in results.Items)
            {
                data.Results.Add(MapResult<T>(result));
            }
            return data;
        }

        protected T MapResult<T>(SearchResult result)
        {
            //TODO - how to handle this generically
            if (typeof(T) == typeof(Teaser))
            {
                var teaser = new Teaser();
                var url = ContentResolver != null ? ContentResolver.ResolveLink(result.Url) : result.Url;
                teaser.Headline = result.Title;
                teaser.Link = new Link { Url = url };
                teaser.Text = result.Summary;
                return (T)Convert.ChangeType(teaser, typeof(T));
            }
            return default(T);
        }

        private NameValueCollection SetupParameters(NameValueCollection parameters)
        {
            //parameters.Add("fq", "publicationid:" + WebRequestContext.Localization.LocalizationId);
            var result = new NameValueCollection();
            foreach(var key in parameters.AllKeys)
            {
                string value = "";
                switch(key)
                {
                    case "q":
                        value = String.Format("\"{0}\"", parameters[key]);
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
    }
}