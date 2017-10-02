using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel
{
    public interface ITemplate : IRepositoryLocal
    {
        IOrganizationalItem Folder { get; }
        IFieldSet MetadataFields { get; }
        DateTime RevisionDate { get; }
    }
}
