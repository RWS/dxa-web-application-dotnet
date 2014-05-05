using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sdl.Web.Mvc.Models
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SemanticEntityAttribute : Attribute
    {
        public SemanticEntityAttribute(string vocab) : this(vocab, null) { }
        public SemanticEntityAttribute(string vocab, string entityName) : this(vocab,entityName,"", true){}
        public SemanticEntityAttribute(string vocab, string entityName, string prefix, bool isDefaultVocab = false)
        {
            Vocab = vocab;
            EntityName = entityName;
            Prefix = prefix;
            IsDefaultVocab = isDefaultVocab;
        }
        public string Vocab { get; set; }
        public string Prefix { get; set; }
        public string EntityName { get; set; }
        public bool IsDefaultVocab { get; set; }
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
        public SemanticPropertyAttribute(string property)
        {
            PropertyName = property;
        }
        public string PropertyName { get; set; }
        public override object TypeId
        {
            get
            {
                return this;
            }
        }
    }
}
