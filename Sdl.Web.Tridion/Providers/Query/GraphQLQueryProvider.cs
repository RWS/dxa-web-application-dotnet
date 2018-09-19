using System.Collections.Generic;
using Sdl.Web.Common.Models;
using Sdl.Web.PublicContentApi.ContentModel;
using Sdl.Web.PublicContentApi.Utils;
using Sdl.Web.Tridion.PCAClient;
using System;
using System.Linq;

namespace Sdl.Web.Tridion.Providers.Query
{   
    public class GraphQLQueryProvider : Common.Interfaces.IQueryProvider
    {
        public bool HasMore { get; set; }

        public string Cursor { get; set; }

        public IEnumerable<string> ExecuteQuery(SimpleBrokerQuery queryParams)
        {
            InputItemFilter filter = BuildFilter(queryParams);
            InputSortParam sort = BuildSort(queryParams);
            var client = PCAClientFactory.Instance.CreateClient();
            var results = client.ExecuteItemQuery(filter, sort, new Pagination
            {
                First = queryParams.PageSize + 1,
                After = queryParams.Cursor
            }, null, null, false);

            HasMore = results.Edges.Count > queryParams.PageSize;
            int n = HasMore ? queryParams.PageSize : results.Edges.Count;
            var resultList = new List<string>();
            for (int i = 0; i < n; i++)
            {
                resultList.Add(results.Edges[i].Node.CmUri());
            }
            Cursor = n > 0 ? results.Edges[n - 1].Cursor : null;
            return resultList;
        }

        public IEnumerable<IItem> ExecuteQueryItems(SimpleBrokerQuery queryParams)
        {
            InputItemFilter filter = BuildFilter(queryParams);
            InputSortParam sort = BuildSort(queryParams);
            var client = PCAClientFactory.Instance.CreateClient();
            var results = client.ExecuteItemQuery(filter, sort, new Pagination
            {
                First = queryParams.PageSize + 1,
                After = queryParams.Cursor
            }, null, null, false);

            HasMore = results.Edges.Count > queryParams.PageSize;
            int n = HasMore ? queryParams.PageSize : results.Edges.Count;
            var resultList = results.Edges.Select(edge => edge.Node).ToList();
            Cursor = n > 0 ? results.Edges[n - 1].Cursor : null;
            return resultList;
        }

        protected InputItemFilter BuildFilter(SimpleBrokerQuery queryParams)
        {
            InputItemFilter filter = new InputItemFilter
            {
                ItemTypes = new List<ItemType> {ItemType.COMPONENT},
            };
            if (queryParams.SchemaId > 0)
            {
                filter.Schema = new InputSchemaCriteria {Id = queryParams.SchemaId};
            }
            if (queryParams.PublicationId > 0)
            {
                filter.PublicationIds = new List<int?> { queryParams.PublicationId};
            }           
            return filter;
        }

        protected InputSortParam BuildSort(SimpleBrokerQuery queryParams)
        {
            if (!string.IsNullOrEmpty(queryParams.Sort) && queryParams.Sort.ToLower() != "none")
            {
                InputSortParam sort = new InputSortParam();
                sort.Order = queryParams.Sort.ToLower().EndsWith("asc") ? 
                    SortOrderType.Ascending : SortOrderType.Descending;

                int idx = queryParams.Sort.Trim().IndexOf(" ", StringComparison.Ordinal);
                string sortColumn = idx > 0 ? queryParams.Sort.Trim().Substring(0, idx) : queryParams.Sort.Trim();
                switch (sortColumn.ToLower())
                {
                    case "title":
                        sort.SortBy = SortFieldType.TITLE;
                        break;
                    case "pubdate":
                        sort.SortBy = SortFieldType.LAST_PUBLISH_DATE;
                        break;
                    default:
                        sort.SortBy = SortFieldType.CREATION_DATE;
                        break;
                }

                return sort;
            }
            return null;
        }
    }
}
