using System;
using System.Collections.Generic;
using System.Linq;
using Tridion.ContentDelivery.DynamicContent.Query;
using DD4T.ContentModel;

namespace DD4T.Providers.SDLTridion2011sp1
{
    public class ExtendedQueryParameters : ITridionQueryWrapper
    {
        public enum QueryLogic
        {
            AllCriteriaMatch,
            AnyCriteriaMatch
        }

        public enum MetaQueryOrder
        {
            Ascending,
            Descending
        }


        public string[] QuerySchemas { get; set; }
        public IList<MetaQueryItem> MetaQueryValues { get; set; }
        public QueryLogic MetaQueryLogic { get; set; }

        public IList<KeywordItem> KeywordValues { get; set; }
        public QueryLogic KeywordQueryLogic { get; set; }

        public DateTime LastPublishedDate { get; set; }
        public string QuerySortField { get; set; }
        public MetaQueryOrder QuerySortOrder { get; set; }
        public int MaximumComponents { get; set; }
        public int PublicationId { get; set; }
        public MetadataType SortType { get; set; }

        public ExtendedQueryParameters()
        {
            // Default all parameters
            QuerySchemas = new string[]{};
            MetaQueryValues = new List<MetaQueryItem>();
            MetaQueryLogic = QueryLogic.AllCriteriaMatch;

            KeywordValues = new List<KeywordItem>();
            KeywordQueryLogic = QueryLogic.AllCriteriaMatch;

            LastPublishedDate = DateTime.MinValue;

            QuerySortField = "ItemTitle";
            SortType = MetadataType.STRING;
            QuerySortOrder = MetaQueryOrder.Ascending;
            MaximumComponents = int.MaxValue;
        }




        public Tridion.ContentDelivery.DynamicContent.Query.Query ToTridionQuery()
        {
            string[] basedOnSchemas = QuerySchemas;
            DateTime lastPublishedDate = LastPublishedDate;
            IList<MetaQueryItem> metaQueryItems = MetaQueryValues;
            QueryLogic metaQueryLogic = MetaQueryLogic;
            int maxmimumComponents = MaximumComponents;

            Query q = null;
            //PublicationCriteria publicationAndLastPublishedDateCriteria = new PublicationCriteria(PublicationId);
            PublicationCriteria publicationAndLastPublishedDateCriteria = new PublicationCriteria(PublicationId);
            //format DateTime // 00:00:00.000
            ItemLastPublishedDateCriteria dateLastPublished = new ItemLastPublishedDateCriteria(lastPublishedDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), Criteria.GreaterThanOrEqual);
            //publicationAndLastPublishedDateCriteria.AddCriteria(dateLastPublished);

            Criteria basedOnSchemaAndInPublication;

            if (basedOnSchemas.Length > 0)
            {
                Criteria[] schemaCriterias = new Criteria[basedOnSchemas.Length];
                int i = 0;
                foreach (var schema in basedOnSchemas)
                {
                    TcmUri schemaUri = new TcmUri(schema);
                    schemaCriterias.SetValue(new ItemSchemaCriteria(schemaUri.ItemId), i);
                    i++;
                }
                Criteria basedOnSchema = CriteriaFactory.Or(schemaCriterias);
                basedOnSchemaAndInPublication = CriteriaFactory.And(publicationAndLastPublishedDateCriteria, basedOnSchema);
            }
            else
            {
                basedOnSchemaAndInPublication = publicationAndLastPublishedDateCriteria;
            }

            // Add filtering for meta data
            Criteria schemasAndMetaData;
            if (metaQueryItems.Count > 0)
            {
                Criteria metaQuery;
                Criteria[] metaCriterias = new Criteria[metaQueryItems.Count];
                int metaCount = 0;
                foreach (MetaQueryItem queryItem in metaQueryItems)
                {
                    CustomMetaKeyCriteria metaField = new CustomMetaKeyCriteria(queryItem.MetaField);
                    CustomMetaValueCriteria metaCriteria;
                    FieldOperator metaOperator = typeof(Criteria).GetField(queryItem.MetaOperator.ToString()).GetValue(null) as FieldOperator;

                    switch (queryItem.MetaValue.GetType().Name)
                    {
                        case "DateTime":
                            DateTime tempDate = (DateTime)queryItem.MetaValue;
                            metaCriteria = new CustomMetaValueCriteria(metaField, tempDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), "yyyy-MM-dd HH:mm:ss.SSS", metaOperator);
                            break;
                        case "Float":
                            metaCriteria = new CustomMetaValueCriteria(metaField, (float)queryItem.MetaValue, metaOperator);
                            break;
                        case "String":
                            metaCriteria = new CustomMetaValueCriteria(metaField, queryItem.MetaValue as string, metaOperator);
                            break;
                        default:
                            throw new System.Exception("Unexpected query item data type; " + queryItem.MetaValue.GetType().Name);
                    }

                    metaCriterias.SetValue(metaCriteria, metaCount);
                    metaCount++;
                }

                if (MetaQueryLogic == QueryLogic.AllCriteriaMatch)
                {
                    metaQuery = CriteriaFactory.And(metaCriterias);
                }
                else
                {
                    metaQuery = CriteriaFactory.Or(metaCriterias);
                }
                schemasAndMetaData = CriteriaFactory.And(basedOnSchemaAndInPublication, metaQuery);
            }
            else
            {
                schemasAndMetaData = basedOnSchemaAndInPublication;
            }

            Criteria allConditions;
            if (KeywordValues.Count > 0)
            {
                Criteria[] keywordCriterias = new Criteria[KeywordValues.Count];
                int keywordCount = 0;
                foreach (KeywordItem keyCriteria in KeywordValues)
                {
                    TaxonomyKeywordCriteria keywordField = new TaxonomyKeywordCriteria(keyCriteria.CategoryUri, keyCriteria.KeywordUri, false);
                    keywordCriterias.SetValue(keywordField, keywordCount);
                    keywordCount++;
                }

                Criteria keyQuery;
                if (KeywordQueryLogic == QueryLogic.AllCriteriaMatch)
                {
                    keyQuery = CriteriaFactory.And(keywordCriterias);
                }
                else
                {
                    keyQuery = CriteriaFactory.Or(keywordCriterias);
                }
                allConditions = CriteriaFactory.And(schemasAndMetaData, keyQuery);
            }
            else
            {
                allConditions = schemasAndMetaData;
            }


            q = new Query(allConditions);
            if (maxmimumComponents != 0 && maxmimumComponents != int.MaxValue)
            {
                LimitFilter limitResults = new LimitFilter(maxmimumComponents);
                q.SetResultFilter(limitResults);
            }

            // Sort column should either be a standard or custom metaData field
            SortColumn paramSort;
            if (typeof(SortParameter).GetField(QuerySortField) != null)
            {
                paramSort = typeof(SortParameter).GetField(QuerySortField).GetValue(null) as SortColumn;
            }
            else
            {
                // Why do we need to tell Tridion what data type the field is! Its in the database already!
                paramSort = new CustomMetaKeyColumn(QuerySortField, typeof(MetadataType).GetField(SortType.ToString()).GetValue(null) as MetadataType);
            }
            SortDirection paramSortDirection = typeof(SortParameter).GetField(QuerySortOrder.ToString()).GetValue(null) as SortDirection;
            SortParameter sortParameter = new SortParameter(paramSort, paramSortDirection);
            q.AddSorting(sortParameter);
            return q;
        }
    }

    public class KeywordItem
    {
        public string KeywordUri { get; set; }
        public string CategoryUri { get; set; }

        public KeywordItem(string category, string keyword)
        {
            this.KeywordUri = keyword;
            this.CategoryUri = category;
        }
    }

    public class MetaQueryItem
    {
        private readonly string[] supportedTypes = { "Float", "DateTime", "String" };
        private object metaValueData;

        public enum QueryOperator
        {
            Equal,
            NotEqual,
            LessThanOrEqual,
            LessThan,
            GreaterThanOrEqual,
            GreaterThan,
            Like
        }

        public string MetaField { get; set; }
        public object MetaValue
        {
            get { return metaValueData; }
            set
            {
                if (!supportedTypes.Contains(value.GetType().Name))
                {
                    throw new Exception("MetaData querying only supports the types; " + string.Join(", ", supportedTypes));
                }
                metaValueData = value;
            }
        }
        public QueryOperator MetaOperator { get; set; }

        public MetaQueryItem(string fieldName, object fieldValue): this(fieldName, fieldValue, QueryOperator.Equal)
        {
        }

        public MetaQueryItem(string fieldName, object fieldValue, QueryOperator fieldOperator)
        {
            MetaField = fieldName;
            MetaValue = fieldValue;
            MetaOperator = fieldOperator;
        }
    }
}
