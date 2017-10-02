namespace DD4T.ContentModel
{
    #region Usings
    using System.Collections.Generic;
    using System;
    #endregion Usings

    public interface IPageTemplate : ITemplate
    {
        string FileExtension { get; }
    }
}
