namespace DD4T.ContentModel
{
    using System.Collections.Generic;
    using System;

    public interface IComponent : IRepositoryLocal, IViewable
    {
        [Obsolete]
        IList<ICategory> Categories { get; }
        ComponentType ComponentType { get; }
        IFieldSet Fields { get; }
        //IDictionary<string,IField> Fields { get; }
        IOrganizationalItem Folder { get; }
        IFieldSet MetadataFields { get; }
        //IDictionary<string, IField> MetadataFields { get; }
        IMultimedia Multimedia { get; }
        ISchema Schema { get; }
        //string ResolvedUrl { get; set; }
        int Version { get; }
        DateTime LastPublishedDate { get; }
        DateTime RevisionDate { get; }
        string EclId { get; }
    }
}
