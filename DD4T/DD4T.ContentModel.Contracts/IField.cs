namespace DD4T.ContentModel
{
    #region Usings
    using System;
    using System.Collections.Generic;
    #endregion Usings

    public interface IField
    {
        string CategoryId { get; }
        string CategoryName { get; }
        IList<DateTime> DateTimeValues { get; }
        IList<IFieldSet> EmbeddedValues { get; }
        ISchema EmbeddedSchema { get; }
        FieldType FieldType { get; }
        IList<IComponent> LinkedComponentValues { get; }
        string Name { get; }
        IList<double> NumericValues { get; }
        IList<string> Values { get; }
        string Value { get; }
        string XPath { get; }
        IList<IKeyword> KeywordValues { get; }
        IList<IKeyword> Keywords { get; }
    }
}
