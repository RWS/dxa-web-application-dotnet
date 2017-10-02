namespace DD4T.ContentModel
{
    using System;
    using System.Collections.Generic;

    public interface IComponentMeta
    {
        DateTime ModificationDate { get; }
        DateTime CreationDate { get; }
        DateTime LastPublishedDate { get; }
    }
}
