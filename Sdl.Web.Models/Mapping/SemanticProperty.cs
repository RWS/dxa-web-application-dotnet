using System;

namespace Sdl.Web.Mvc.Mapping
{
    public class SemanticProperty
    {
        // field semantics {"vocab":"s","property":"headline"}
        // schema semantics {"vocab":"s","entity":"Article"}
        public string Vocab { get; set; }
        public string PropertyName { get; set; }
        public string EntityName { get; set; }

        //public bool Equals(SemanticProperty semanticProperty)
        //{
        //    // check on PropertyName
        //    if (!string.IsNullOrEmpty(semanticProperty.PropertyName) && !string.IsNullOrEmpty(PropertyName) && semanticProperty.PropertyName.Equals(PropertyName))
        //    {
        //        return semanticProperty.Vocab.Equals(Vocab);
        //    }
        //    // check on EntityName
        //    if (!string.IsNullOrEmpty(semanticProperty.EntityName) && !string.IsNullOrEmpty(EntityName) && semanticProperty.EntityName.Equals(EntityName))
        //    {
        //        return semanticProperty.Vocab.Equals(Vocab);
        //    }
        //    return false;
        //}
    }
}
