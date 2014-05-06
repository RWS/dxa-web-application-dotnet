using System;

namespace Sdl.Web.Mvc.Mapping
{
    public class FieldSemantics : SchemaSemantics
    {
        // field semantics {"Prefix":"s","Entity":"Article","Property":"headline"}
        // Prefix and Entity inherited from SchemaSemantics 
        public string Property { get; set; }
    }
}
