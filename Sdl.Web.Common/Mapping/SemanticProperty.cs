using System;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents a Semantic Property.
    /// </summary>
    public class SemanticProperty
    {
        public const string AllFields = "_all";
        public const string Self = "_self";

        public string Prefix { get; }
        public string PropertyName { get; }
        public SemanticType SemanticType { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="name"></param>
        [Obsolete("Deprecated in DXA 2.0. Use constructor with three parameters instead.")] // TODO TSI-878: Remove this constructor completely (?)
        public SemanticProperty(string prefix, string name)
        {
            Prefix = prefix;
            PropertyName = name;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="propertyName"></param>
        /// <param name="semanticType"></param>
        public SemanticProperty(string prefix, string propertyName, SemanticType semanticType)
        {
            Prefix = prefix;
            PropertyName = propertyName;
            SemanticType = semanticType;
        }

        public override string ToString()
            => $"{Prefix}:{PropertyName} ({SemanticType})";
    }
}
