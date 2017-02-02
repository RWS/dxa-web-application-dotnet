using System;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Common.Models
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class SemanticDefaultsAttribute : Attribute
    {
        public SemanticDefaultsAttribute() : this(ViewModel.CoreVocabulary) { }
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
        public SemanticEntityAttribute()
        {
            Vocab = SemanticMapping.DefaultVocabulary;
        }

        public SemanticEntityAttribute(string vocab, string entityName, string prefix = null)
        {
            Vocab = vocab ?? SemanticMapping.DefaultVocabulary;
            EntityName = entityName;
            Prefix = prefix ?? string.Empty;
        }
        public string Vocab { get; set; }
        public string Prefix { get; set; }
        public string EntityName { get; set; }
        public bool Public { get; set; }
        public override object TypeId => this;
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
