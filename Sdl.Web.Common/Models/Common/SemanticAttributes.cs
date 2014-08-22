using System;

namespace Sdl.Web.Common.Models
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SemanticDefaultsAttribute : Attribute
    {
        public SemanticDefaultsAttribute() : this(EntityBase.CoreVocabulary) { }
        public SemanticDefaultsAttribute(string vocab) : this(vocab, String.Empty) { }
        public SemanticDefaultsAttribute(string vocab, string prefix) : this(vocab, prefix, true) { }
        public SemanticDefaultsAttribute(string vocab, string prefix, bool mapAllProperties)
        {
            Vocab = vocab;
            Prefix = prefix;
            MapAllProperties = mapAllProperties;
        }
        public string Vocab { get; set; }
        public string Prefix { get; set; }
        public bool MapAllProperties { get; set; }
    }
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SemanticEntityAttribute : Attribute
    {
        public SemanticEntityAttribute() { }
        public SemanticEntityAttribute(string vocab) : this(vocab, null) { }
        public SemanticEntityAttribute(string vocab, string entityName) : this(vocab, entityName, String.Empty) { }
        public SemanticEntityAttribute(string vocab, string entityName, string prefix)
        {
            Vocab = vocab;
            EntityName = entityName;
            Prefix = prefix;
        }
        public string Vocab { get; set; }
        public string Prefix { get; set; }
        public string EntityName { get; set; }
        public bool Public { get; set; }
        public override object TypeId
        {
            get
            {
                return this;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class SemanticPropertyAttribute : Attribute
    {
        public SemanticPropertyAttribute() { }
        public SemanticPropertyAttribute(string property)
        {
            PropertyName = property;
        }
        public SemanticPropertyAttribute(bool ignoreMapping)
        {
            IgnoreMapping = ignoreMapping;
        }
        public string PropertyName { get; set; }
        public bool IgnoreMapping { get; set; }
        public override object TypeId
        {
            get
            {
                return this;
            }
        }
    }
}
