using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace DD4T.ContentModel
{
    public class ComponentMeta : IComponentMeta
    {
        public string ID { get; set; }
        public DateTime ModificationDate { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastPublishedDate { get; set; }

        DateTime IComponentMeta.ModificationDate
        {
            get { return ModificationDate; }
        }

        DateTime IComponentMeta.CreationDate
        {
            get { return CreationDate; }
        }

        DateTime IComponentMeta.LastPublishedDate
        {
            get { return LastPublishedDate; }
        }
    }

    public class Page : RepositoryLocalItem, IPage
    {
        public DateTime RevisionDate { get; set; }
        public string Filename { get; set; }
        public DateTime LastPublishedDate { get; set; }

        public PageTemplate PageTemplate { get; set; }

        [XmlIgnore]
        IPageTemplate IPage.PageTemplate
        {
            get { return PageTemplate; }
        }

        public Schema Schema { get; set; }

        [XmlIgnore]
        ISchema IPage.Schema
        {
            get { return Schema; }
        }

        public FieldSet MetadataFields { get; set; }

        [XmlIgnore]
        IFieldSet IPage.MetadataFields
        {
            get
            {
                return MetadataFields != null ? MetadataFields as IFieldSet : null;
            }
        }

        public List<ComponentPresentation> ComponentPresentations { get; set; }

        [XmlIgnore]
        IList<IComponentPresentation> IPage.ComponentPresentations
        {
            get { return ComponentPresentations.ToList<IComponentPresentation>(); }
        }

        public OrganizationalItem StructureGroup { get; set; }

        [XmlIgnore]
        IOrganizationalItem IPage.StructureGroup
        {
            get { return StructureGroup; }
        }

        public List<Category> Categories { get; set; }

        [XmlIgnore]
        IList<ICategory> IPage.Categories
        {
            get { return Categories.ToList<ICategory>(); }
        }

        public int Version { get; set; }
    }

    public class Keyword : RepositoryLocalItem, IKeyword
    {
        [XmlAttribute]
        public bool IsRoot { get; set; }

        [XmlAttribute]
        public bool IsAbstract { get; set; }

        [XmlAttribute]
        public string Description { get; set; }

        [XmlAttribute]
        public string Key { get; set; }

        [XmlAttribute]
        public string TaxonomyId { get; set; }

        [XmlAttribute]
        public string Path { get; set; }

        [XmlIgnore]
        public IList<IKeyword> RelatedKeywords { get; set; }

        [XmlIgnore]
        public IList<IKeyword> ParentKeywords { get; set; }

        public FieldSet MetadataFields { get; set; }

        [XmlIgnore]
        IFieldSet IKeyword.MetadataFields
        {
            get { return MetadataFields != null ? (MetadataFields as IFieldSet) : null; }
        }

        [XmlIgnore]
        public ISchema MetadataSchema { get; set; }

        public Keyword()
        {
            this.MetadataFields = new FieldSet();
            this.ParentKeywords = new List<IKeyword>();
            this.RelatedKeywords = new List<IKeyword>();
        }
    }

    public class Category : RepositoryLocalItem, ICategory
    {
        public List<Keyword> Keywords { get; set; }

        [XmlIgnore]
        IList<IKeyword> ICategory.Keywords
        { get { return Keywords.ToList<IKeyword>(); } }
    }

    public class ComponentPresentation : Model, IComponentPresentation
    {
        [XmlIgnore]
        public IPage Page { get; set; }

        public Component Component { get; set; }

        [XmlIgnore]
        IComponent IComponentPresentation.Component
        {
            get { return Component as IComponent; }
        }

        public ComponentTemplate ComponentTemplate { get; set; }

        [XmlIgnore]
        IComponentTemplate IComponentPresentation.ComponentTemplate
        {
            get { return ComponentTemplate as IComponentTemplate; }
        }

        public string RenderedContent { get; set; }
        public bool IsDynamic { get; set; }

        [XmlIgnore]
        public int OrderOnPage { get; set; }

        [Obsolete("Conditions is deprecated, please refactor your code to work with TargetGroup.Conditions from items within the TargetGroupConditions property")]
        public List<Condition> Conditions
        {
            get
            {
                if (TargetGroupConditions!=null)
                {
                    //This is for backwards compatibility where for some reason the conditions of the target groups were pulled out and put
                    //On the ComponentPresentation model (skipping out the top level of target group definition; ie the target group and its inclusion/exclusion for the CP)
                    var conditions = TargetGroupConditions.Select(t => t.TargetGroup.Conditions).SelectMany(x => x).ToList();
                    return conditions.Count > 0 ? conditions : null;
                }
                return null;
            }
            set
            {
                if (value != null && value.Count > 0)
                {
                    //For backwards compatibility on content published before TargetGroupConditions was introduced
                    //We need to 'fake' a TargetGroupCondition with the given conditions
                    TargetGroup dummyTargetGroup = new TargetGroup();
                    dummyTargetGroup.Conditions = value;
                    dummyTargetGroup.Title = "Please republish page to update with actual Target Group data";
                    TargetGroupCondition condition = new TargetGroupCondition() { TargetGroup = dummyTargetGroup };
                    this.TargetGroupConditions = new List<TargetGroupCondition> { condition };
                }
            }
        }

        public List<TargetGroupCondition> TargetGroupConditions { get; set; }

        [XmlIgnore]
        [Obsolete("Conditions is deprecated, please refactor your code to work with TargetGroup.Conditions from items within the TargetGroupConditions property")]
        IList<ICondition> IComponentPresentation.Conditions
        {
            get { return TargetGroupConditions==null ? null : TargetGroupConditions.Select(t => t.TargetGroup.Conditions).SelectMany(x => x).ToList<ICondition>(); }
        }

        [XmlIgnore]
        IList<ITargetGroupCondition> IComponentPresentation.TargetGroupConditions
        {
            get { return TargetGroupConditions==null ? null : TargetGroupConditions.ToList<ITargetGroupCondition>(); }
        }
    }

    public class PageTemplate : RepositoryLocalItem, IPageTemplate
    {
        public string FileExtension { get; set; }
        public DateTime RevisionDate { get; set; }
        public FieldSet MetadataFields { get; set; }

        [XmlIgnore]
        IFieldSet ITemplate.MetadataFields
        {
            get
            {
                return MetadataFields != null ? MetadataFields as IFieldSet : null;
            }
        }

        public OrganizationalItem Folder { get; set; }

        [XmlIgnore]
        IOrganizationalItem ITemplate.Folder
        {
            get { return Folder as IOrganizationalItem; }
        }
    }

    public class ComponentTemplate : RepositoryLocalItem, IComponentTemplate
    {
        public string OutputFormat { get; set; }
        public DateTime RevisionDate { get; set; }
        public FieldSet MetadataFields { get; set; }

        [XmlIgnore]
        IFieldSet ITemplate.MetadataFields
        {
            get
            {
                return MetadataFields != null ? MetadataFields as IFieldSet : null;
            }
        }

        public OrganizationalItem Folder { get; set; }

        [XmlIgnore]
        IOrganizationalItem ITemplate.Folder
        {
            get { return Folder as IOrganizationalItem; }
        }
    }

    public class Component : RepositoryLocalItem, IComponent
    {
        #region Properties

        public DateTime LastPublishedDate { get; set; }
        public DateTime RevisionDate { get; set; }
        public Schema Schema { get; set; }

        [XmlIgnore]
        ISchema IComponent.Schema
        {
            get { return Schema; }
        }

        public FieldSet Fields { get; set; }

        [XmlIgnore]
        IFieldSet IComponent.Fields
        {
            get { return Fields != null ? (Fields as IFieldSet) : null; }
        }

        public FieldSet MetadataFields { get; set; }

        [XmlIgnore]
        IFieldSet IComponent.MetadataFields
        {
            get { return MetadataFields != null ? (MetadataFields as IFieldSet) : null; }
        }

        public ComponentType ComponentType { get; set; }
        public Multimedia Multimedia { get; set; }

        [XmlIgnore]
        IMultimedia IComponent.Multimedia
        {
            get { return Multimedia as IMultimedia; }
        }

        public OrganizationalItem Folder { get; set; }

        [XmlIgnore]
        IOrganizationalItem IComponent.Folder
        {
            get { return Folder as IOrganizationalItem; }
        }

        public List<Category> Categories { get; set; }

        [XmlIgnore]
        IList<ICategory> IComponent.Categories
        {
            get { return Categories.ToList<ICategory>(); } //as IList<ICategory>;
        }

        public int Version { get; set; }

        public string EclId { get; set; }

        #endregion Properties

        #region constructors

        public Component()
        {
            this.Categories = new List<Category>();
            this.Schema = new Schema();
            this.Fields = new FieldSet();
            this.MetadataFields = new FieldSet();
        }

        #endregion constructors
    }

    public class Schema : RepositoryLocalItem, ISchema
    {
        public OrganizationalItem Folder { get; set; }

        [XmlIgnore]
        IOrganizationalItem ISchema.Folder
        {
            get { return Folder as IOrganizationalItem; }
        }

        public string RootElementName
        {
            get;
            set;
        }
    }

    public enum MergeAction { Replace, Merge, MergeMultiValueSkipSingleValue, MergeMultiValueReplaceSingleValue, Skip }

    [Serializable]
    public class FieldSet : SerializableDictionary<string, IField, Field>, IFieldSet, IXmlSerializable
    {
    }

    public class Field : IField
    {
        #region JSON serialization control

        // NOTE: we're simply supressing some properties from JSON serialization here. Normally you would use the [JsonIgnore] attribute for that purpose.
        // However, we are using JSON.NET conditional serialization feature (ShouldSerializeXYZ) here to avoid a direct reference to JSON.NET.

        /// <summary>
        /// Supresses JSON serialization of the <see cref="Value"/> property (which is only a convenience property derived from <see cref="Values"/>)
        /// </summary>
        public bool ShouldSerializeValue()
        {
            return false;
        }

        /// <summary>
        /// Supresses JSON serialization of the <see cref="Keywords"/> property (which is only a legacy property derived from <see cref="KeywordValues"/>)
        /// </summary>
        public bool ShouldSerializeKeywords()
        {
            return false;
        }

        #endregion JSON serialization control

        #region Properties

        public string Name
        {
            get;
            set;
        }

        public string Value
        {
            get
            {
                if (this.Values == null || this.Values.Count == 0)
                    return string.Empty;
                return this.Values[0];
            }
        }

        public List<string> Values
        {
            get;
            set;
        }

        [XmlIgnore]
        IList<string> IField.Values
        {
            get { return Values; }
        }

        public List<double> NumericValues
        {
            get;
            set;
        }

        [XmlIgnore]
        IList<double> IField.NumericValues
        {
            get { return NumericValues; }
        }

        public List<DateTime> DateTimeValues
        {
            get;
            set;
        }

        [XmlIgnore]
        IList<DateTime> IField.DateTimeValues
        {
            get { return DateTimeValues; }
        }

        public List<Component> LinkedComponentValues
        {
            get;
            set;
        }

        [XmlIgnore]
        IList<IComponent> IField.LinkedComponentValues
        {
            get
            {
                return (LinkedComponentValues == null) ? null : LinkedComponentValues.ToList<IComponent>();
            }
        }

        public List<FieldSet> EmbeddedValues
        {
            get;
            set;
        }

        [XmlIgnore]
        IList<IFieldSet> IField.EmbeddedValues
        {
            get
            {
                return (EmbeddedValues == null) ? null : EmbeddedValues.ToList<IFieldSet>();
            }
        }

        public Schema EmbeddedSchema
        {
            get;
            set;
        }

        [XmlIgnore]
        ISchema IField.EmbeddedSchema
        {
            get
            {
                return EmbeddedSchema;
            }
        }

        [XmlAttribute]
        public FieldType FieldType
        {
            get;
            set;
        }

        [XmlAttribute]
        public string CategoryName
        {
            get;
            set;
        }

        [XmlAttribute]
        public string CategoryId
        {
            get;
            set;
        }

        [XmlAttribute]
        public string XPath
        {
            get;
            set;
        }

        [XmlIgnore]
        public List<Keyword> Keywords
        {
            get
            {
                return KeywordValues;
            }
            set
            {
                KeywordValues = value;
            }
        }

        public List<Keyword> KeywordValues
        {
            get;
            set;
        }

        [XmlIgnore]
        IList<IKeyword> IField.Keywords
        {
            get
            {
                return (KeywordValues == null) ? null : KeywordValues.ToList<IKeyword>();
            }
        }

        [XmlIgnore]
        IList<IKeyword> IField.KeywordValues
        {
            get
            {
                return (KeywordValues == null) ? null : KeywordValues.ToList<IKeyword>();
            }
        }

        #endregion Properties

        #region Constructors

        public Field()
        {
            this.Keywords = new List<Keyword>();
            this.Values = new List<string>();
            this.NumericValues = new List<double>();
            this.DateTimeValues = new List<DateTime>();
            this.LinkedComponentValues = new List<Component>();
        }

        /// <summary>
        /// Initializes a new <see cref="Field"/> instance with a given name and value.
        /// </summary>
        /// <param name="name">The name of the field.</param>
        /// <param name="value">The value. Will be mapped to <see cref="Values"/>, <see cref="NumericValues"/> or <see cref="DateTimeValues"/> depending on its type.</param>
        /// <remarks>
        /// Used by <see cref="Model.AddExtensionProperty"/> implementation.
        /// Note that this initializes only the necessary properties to keep the serialized data small.
        /// </remarks>
        internal Field(string name, object value)
        {
            Name = name;

            if (value is IEnumerable && !(value is string))
            {
                foreach (object item in (IEnumerable)value)
                {
                    AddFieldValue(item);
                }
            }
            else if (value != null)
            {
                AddFieldValue(value);
            }
        }

        #endregion Constructors

        internal void AddFieldValue(object value)
        {
            if (value is int || value is long || value is double)
            {
                if (NumericValues == null)
                {
                    NumericValues = new List<double>();
                }
                NumericValues.Add(Convert.ToDouble(value));
                FieldType = FieldType.Number;
            }
            else if (value is DateTime)
            {
                if (DateTimeValues == null)
                {
                    DateTimeValues = new List<DateTime>();
                }
                DateTimeValues.Add((DateTime)value);
                FieldType = FieldType.Date;
            }
            else
            {
                if (Values == null)
                {
                    Values = new List<string>();
                }
                Values.Add(value.ToString());
                FieldType = FieldType.Text;
            }
        }
    }

    public abstract class Model : IModel
    {
        public SerializableDictionary<string, IFieldSet, FieldSet> ExtensionData { get; set; }

        [XmlIgnore]
        IDictionary<string, IFieldSet> IModel.ExtensionData
        {
            get { return ExtensionData; }
        }

        public void AddExtensionProperty(string sectionName, string propertyName, object value)
        {
            if (value == null)
            {
                // For a null value we just don't do anything rather than creating a field with a null value (or add a null value to existing field).
                return;
            }

            if (ExtensionData == null)
            {
                ExtensionData = new SerializableDictionary<string, IFieldSet, FieldSet>();
            }

            IFieldSet sectionFieldSet;
            if (!ExtensionData.TryGetValue(sectionName, out sectionFieldSet))
            {
                sectionFieldSet = new FieldSet();
                ExtensionData.Add(sectionName, sectionFieldSet);
            }

            IField propertyField;
            if (!sectionFieldSet.TryGetValue(propertyName, out propertyField))
            {
                propertyField = new Field(propertyName, value);
                sectionFieldSet.Add(propertyName, propertyField);
            }
            else
            {
                ((Field)propertyField).AddFieldValue(value);
            }
        }
    }

    public abstract class TridionItem : Model, IItem
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    public abstract class RepositoryLocalItem : TridionItem, IRepositoryLocal
    {
        public string PublicationId { get; set; }
        public Publication Publication { get; set; }

        [XmlIgnore]
        IPublication IRepositoryLocal.Publication
        {
            get { return Publication; }
        }

        public Publication OwningPublication { get; set; }

        [XmlIgnore]
        IPublication IRepositoryLocal.OwningPublication
        {
            get { return OwningPublication; }
        }
    }

    public class OrganizationalItem : RepositoryLocalItem, IOrganizationalItem
    {
    }

    public class Publication : TridionItem, IPublication
    {
    }

    public class TcmUri
    {
        public int ItemId { get; set; }
        public int PublicationId { get; set; }
        public int ItemTypeId { get; set; }
        public int Version { get; set; }

        [DebuggerStepThrough]
        public TcmUri(string Uri)
        {
            Regex re = new Regex(@"tcm:(\d+)-(\d+)-?(\d*)-?v?(\d*)");
            Match m = re.Match(Uri);
            if (m.Success)
            {
                PublicationId = Convert.ToInt32(m.Groups[1].Value);
                ItemId = Convert.ToInt32(m.Groups[2].Value);
                if (m.Groups.Count > 3 && !string.IsNullOrEmpty(m.Groups[3].Value))
                {
                    ItemTypeId = Convert.ToInt32(m.Groups[3].Value);
                }
                else
                {
                    ItemTypeId = 16;
                }
                if (m.Groups.Count > 4 && !string.IsNullOrEmpty(m.Groups[4].Value))
                {
                    Version = Convert.ToInt32(m.Groups[4].Value);
                }
                else
                {
                    Version = 0;
                }
            }
        }

        public TcmUri(int PublicationId, int ItemId, int ItemTypeId, int Version)
        {
            this.PublicationId = PublicationId;
            this.ItemId = ItemId;
            this.ItemTypeId = ItemTypeId;
            this.Version = Version;
        }

        public override string ToString()
        {
            if (this.ItemTypeId == 16)
            {
                return string.Format("tcm:{0}-{1}", this.PublicationId, this.ItemId);
            }
            return string.Format("tcm:{0}-{1}-{2}", this.PublicationId, this.ItemId, this.ItemTypeId);
        }

        public static TcmUri NullUri
        {
            get
            {
                return new TcmUri(0, 0, 0, 0);
            }
        }
    }

    public class Multimedia : IMultimedia
    {
        public string Url
        {
            get;
            set;
        }

        public string MimeType
        {
            get;
            set;
        }

        [Obsolete("Please use ViewModels and model any field you like as 'AltText'")]
        public string AltText
        {
            get;
            set;
        }

        public string FileName
        {
            get;
            set;
        }

        public string FileExtension
        {
            get;
            set;
        }

        public long Size
        {
            get;
            set;
        }

        public int Width
        {
            get;
            set;
        }

        public int Height
        {
            get;
            set;
        }
    }

    public class Binary : Component, IBinary
    {
        public byte[] BinaryData { get; set; }
        public string VariantId { get; set; }
        public string Url { get; set; }
        public System.IO.Stream BinaryStream { get; set; }
    }

    public class TargetGroup : RepositoryLocalItem, ITargetGroup
    {
        public string Description { get; set; }

        public List<Condition> Conditions { get; set; }

        [XmlIgnore]
        IList<ICondition> ITargetGroup.Conditions { get { return Conditions.ToList<ICondition>(); } }
    }

    public class Condition : ICondition
    {
        public bool Negate { get; set; }
    }

    public class KeywordCondition : Condition
    {
        public Keyword Keyword { get; set; }
        public NumericalConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }

    public class CustomerCharacteristicCondition : Condition
    {
        public string Name { get; set; }
        public ConditionOperator Operator { get; set; }
        public object Value { get; set; }
    }

    public class TargetGroupCondition : Condition, ITargetGroupCondition
    {
        public TargetGroup TargetGroup { get; set; }
        [XmlIgnore]
        ITargetGroup ITargetGroupCondition.TargetGroup
        {
            get
            {
                return TargetGroup as ITargetGroup;
            }
        }
    }
}