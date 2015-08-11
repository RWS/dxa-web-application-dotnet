
namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Class for deserialized json schema semantics.
    /// {"Prefix":"s","Entity":"Article"}
    /// </summary>
    public class SchemaSemantics
    {
        /// <summary>
        /// Vocabulary prefix.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Entity name.
        /// </summary>
        public string Entity { get; set; }

        /// <summary>
        /// Initializes a new empty instance of the <see cref="SchemaSemantics"/> class.
        /// </summary>
        public SchemaSemantics() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaSemantics"/> class, using default semantic vocabulary prefix.
        /// </summary>
        /// <param name="entity">Entity name.</param>
        public SchemaSemantics(string entity) : this(SemanticMapping.DefaultPrefix, entity) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaSemantics"/> class.
        /// </summary>
        /// <param name="prefix">Vocabulary prefix</param>
        /// <param name="entity">Entity name</param>
        public SchemaSemantics(string prefix, string entity)
        {
            Prefix = prefix;
            Entity = entity;
        }
    }
}