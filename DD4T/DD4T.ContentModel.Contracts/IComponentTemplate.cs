namespace DD4T.ContentModel
{
    using System.Collections.Generic;
    using System;

    public interface IComponentTemplate : ITemplate
    {
        string OutputFormat { get; }
    }
}
