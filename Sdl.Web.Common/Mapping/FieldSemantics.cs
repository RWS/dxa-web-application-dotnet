
namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Class for deserialized json field semantics.
    /// {"Prefix":"s","Entity":"Article","Property":"headline"}
    /// </summary>
    public class FieldSemantics : SchemaSemantics
    {
        // TODO implement proper override of Equals() and Operator ==

        /// <summary>
        /// Semantic property name.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="FieldSemantics"/> class.
        /// </summary>
        public FieldSemantics() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSemantics"/> class, using default semantic vocabulary prefix.
        /// </summary>
        /// <param name="entity">Entity name</param>
        /// <param name="property">Semantic property name</param>
        public FieldSemantics(string entity, string property) : this(SemanticMapping.DefaultPrefix, entity, property) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSemantics"/> class.
        /// </summary>
        /// <param name="prefix">Vocabulary prefix</param>
        /// <param name="entity">Entity name</param>
        /// <param name="property">Semantic property name</param>
        public FieldSemantics(string prefix, string entity, string property) : base(prefix, entity)
        {
            Property = property;
        }
    }
}