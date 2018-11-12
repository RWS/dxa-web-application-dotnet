using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Extensions;

namespace Sdl.Web.Common.Mapping
{
    /// <summary>
    /// Represents a Semantic Schema Field.
    /// </summary>
    /// <remarks>
    /// Deserialized from JSON in schemas.json.
    /// </remarks>
    public class SemanticSchemaField
    {
        /// <summary>
        /// XML field name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// XML field path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets the XPath used in XPM property metadata
        /// </summary>
        /// <param name="contextXPath">The context XPath (incl. index predicate) for multi-valued embedded fields.</param>
        public string GetXPath(string contextXPath)
        {
            StringBuilder xpathBuilder = new StringBuilder(IsMetadata ? "tcm:Metadata" : "tcm:Content");
            foreach (string pathSegment in Path.Split('/').Skip(1))
            {
                xpathBuilder.Append("/custom:");
                xpathBuilder.Append(pathSegment);
            }
            string xpath = xpathBuilder.ToString();
            if (string.IsNullOrEmpty(contextXPath)) return xpath;

            // merge our xpath with predicates from contextXPath ( [<predicate] )
            string[] parts1 = contextXPath.Split('/');
            string[] parts2 = xpath.Split('/');
            string merged = "";
            for (int i = 0; i < parts2.Length; i++)
            {
                if (i > 0) merged += "/";                
                if (i < parts1.Length)
                {
                    if (parts2[i] == parts1[i].Split('[')[0])
                    {
                        merged += parts1[i];
                    }
                }
                else
                {
                    merged += parts2[i];
                }
            }
            return merged;
        }

        /// <summary>
        /// Is field a metadata field?
        /// </summary>
        public bool IsMetadata => Path.StartsWith("/Metadata");

        /// <summary>
        /// Is field an embedded field?
        /// </summary>
        public bool IsEmbedded => Path.HasNOrMoreOccurancesOfChar(3, '/');

        /// <summary>
        /// Is field multivalued?
        /// </summary>
        public bool IsMultiValue { get; set; }

        /// <summary>
        /// Field semantics.
        /// </summary>
        public List<FieldSemantics> Semantics { get; set; }

        /// <summary>
        /// Embedded fields.
        /// </summary>
        public List<SemanticSchemaField> Fields { get; set; }

        /// <summary>
        /// Initializes an existing instance.
        /// </summary>
        /// <param name="localization"></param>
        public void Initialize(Localization localization)
        {
            foreach (FieldSemantics fieldSemantics in Semantics)
            {
                fieldSemantics.Initialize(localization);
            }
            foreach (SemanticSchemaField field in Fields)
            {
                field.Initialize(localization);
            }
        }

        /// <summary>
        /// Check if current field has given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <returns><c>true</c> if this field has given semantics, <c>false</c> otherwise.</returns>
        public bool HasSemantics(FieldSemantics fieldSemantics) => Semantics.Any(s => s.Equals(fieldSemantics));

        /// <summary>
        /// Find <see cref="SemanticSchemaField"/> with given semantics.
        /// </summary>
        /// <param name="fieldSemantics">The semantics to check against</param>
        /// <param name="includeSelf">If <c>true</c> the field itself will be returned if it matches the given semantics.</param>
        /// <returns>This field or one of its embedded fields that match with the given semantics, null if a match cannot be found</returns>
        public SemanticSchemaField FindFieldBySemantics(FieldSemantics fieldSemantics)
        {
            // Perform a breadth-first lookup: first see if any of the embedded fields themselves match.
            SemanticSchemaField matchingEmbeddedField = Fields.FirstOrDefault(ssf => ssf.HasSemantics(fieldSemantics));
            if (matchingEmbeddedField != null)
            {
                return matchingEmbeddedField;
            }

            // If none of the embedded fields match: let each embedded field do a breadth-first lookup of its embedded fields (recursive).
            return Fields.Select(ssf => ssf.FindFieldBySemantics(fieldSemantics)).FirstOrDefault(matchingField => matchingField != null);
        }

        /// <summary>
        /// Provides a string representation of the object.
        /// </summary>
        /// <returns>A string representation containing the field Name and Path</returns>
        public override string ToString() => $"{GetType().Name} {Name} ({Path})";
    }
}
