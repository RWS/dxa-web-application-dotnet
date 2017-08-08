namespace DD4T.ContentModel
{
    #region Usings
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Xml.Serialization;
    #endregion Usings

    public interface IFieldSet : IDictionary<string, IField>
    {
    }
}
