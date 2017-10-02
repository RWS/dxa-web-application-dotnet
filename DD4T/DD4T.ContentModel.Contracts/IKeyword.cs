using System.Collections.Generic;

namespace DD4T.ContentModel
{
    public interface IKeyword : IRepositoryLocal
    {
        string Path { get; }
        string TaxonomyId { get; }
        string Description { get; }
        string Key { get; }
        IList<IKeyword> ParentKeywords { get; }
        IList<IKeyword> RelatedKeywords { get; }
        IFieldSet MetadataFields { get; }
        ISchema MetadataSchema { get; }
        bool IsAbstract { get; }
        bool IsRoot { get; }
    }
}