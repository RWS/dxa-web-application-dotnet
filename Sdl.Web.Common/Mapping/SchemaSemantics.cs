
namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents the semantics of a Schema: Prefix and Entity.
    /// </summary>
    /// <remarks>
    /// Deserialized from JSON in schemas.json.
    /// </remarks>
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

        #region Constructors

        /// <summary>
        /// Initializes a new empty instance of the <see cref="SchemaSemantics"/> class.
        /// </summary>
        public SchemaSemantics()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaSemantics"/> class, using default semantic vocabulary prefix.
        /// </summary>
        /// <param name="entity">Entity name.</param>
        public SchemaSemantics(string entity)
            : this(SemanticMapping.DefaultPrefix, entity)
        {
        }

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
        #endregion

        /// <summary>
        /// Provides a string representation of the object.
        /// </summary>
        /// <returns>A string representation in format <c>Prefix:Entity</c>.</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", Prefix, Entity);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="SchemaSemantics"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current one.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            FieldSemantics other = obj as FieldSemantics;
            return other != null && Prefix == other.Prefix && Entity == other.Entity;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="SchemaSemantics"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return Prefix.GetHashCode() ^ Entity.GetHashCode();
        }
    }
}