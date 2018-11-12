using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents the semantics of a Schema field: Prefix, Entity and Property.
    /// </summary>
    /// <remarks>
    /// Deserialized from JSON in schemas.json.
    /// </remarks>
    public class FieldSemantics : SchemaSemantics
    {
        /// <summary>
        /// Semantic property name.
        /// </summary>
        public string Property { get; set; }

        #region Constructors

        /// <summary>
        /// Initializes a new empty instance of the <see cref="FieldSemantics"/> class.
        /// </summary>
        /// <remarks>
        /// Used by JSON deserializer.
        /// </remarks>
        public FieldSemantics()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSemantics"/> class.
        /// </summary>
        /// <param name="vocabOrPrefix">Vocabulary prefix</param>
        /// <param name="entity">Entity name</param>
        /// <param name="property">Semantic property name</param>
        /// <param name="localization">The context Localization (used to determine the semantic Vocabulary URI from <paramref name="vocabOrPrefix"/>).</param>
        public FieldSemantics(string vocabOrPrefix, string entity, string property, Localization localization)
            : base(vocabOrPrefix, entity, localization)
        {
            Property = property;
        }
        #endregion

        /// <summary>
        /// Provides a string representation of the object.
        /// </summary>
        /// <returns>A string representation in format <c>Vocab/Prefix:Entity:Property</c>.</returns>
        public override string ToString() => $"{Vocab ?? Prefix}:{Entity}:{Property}";

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="FieldSemantics"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object is equal to the current one.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            FieldSemantics other = obj as FieldSemantics;
            return other != null && base.Equals(other) && Property == other.Property;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="FieldSemantics"/>.
        /// </returns>
        public override int GetHashCode() => base.GetHashCode() ^ Property.GetHashCode();
    }
}