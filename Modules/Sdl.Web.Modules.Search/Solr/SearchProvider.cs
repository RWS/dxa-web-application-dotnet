using Sdl.Web.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Modules.Search.Solr
{
    public class SearchProvider : ISearchProvider
    {
        public SearchResults<Teaser> ExecuteQuery(SearchResults<Teaser> data)
        {
            if (!String.IsNullOrEmpty(data.Query.QueryText))
            {
                //TODO read from config
                Connection conn = new Connection("http://saintjohn01.ams.dev:8080/solr/staging");
                var parameters = new NameValueCollection();
                //Set the query
                parameters["q"] = String.Format("\"{0}\"", data.Query.QueryText);
                //Add highlighting - enables summary field be auto-generated if empty
                parameters.Add("hl", "true");
                //Add publication id - not for now as binaries do not have this field...
                //parameters.Add("fq", "publicationid:" + WebRequestContext.Localization.LocalizationId);
                //Set page number and size
                parameters.Add("start", data.Start.ToString());
                parameters.Add("rows", data.PageSize.ToString());
                var results = conn.ExecuteQuery(parameters);
                data.ItemListElements = results.ItemListElements;
                
                data.Total = results.Total;
            }
            return data;
        }
    }
}