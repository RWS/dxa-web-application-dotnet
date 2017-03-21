using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;

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

            if (string.IsNullOrEmpty(contextXPath))
            {
                return xpath;
            }

            string contextXPathWithoutPredicate = contextXPath.Split('[')[0];
            if (!xpath.StartsWith(contextXPathWithoutPredicate))
            {
                // This should not happen, but if it happens, we just stick with the original XPath.
                Log.Warn("Semantic field's XPath ('{0}') does not match context XPath '{1}'.", xpath, contextXPath);
            }
            return xpath.Replace(contextXPathWithoutPredicate, contextXPath);
        }

        /// <summary>
        /// Is field a metadata field?
        /// </summary>
        public bool IsMetadata
        {
            get
            {
                // metadata fields start their Path with /Metadata
                return Path.StartsWith("/Metadata");
            }
        }

        /// <summary>
        /// Is field an embedded field?
        /// </summary>
        public bool IsEmbedded
        {
            // TODO this could also be a linked field, does that matter?
            get 
            {
                // path of an embedded field contains more than two forward slashes, 
                // e.g. /Article/articleBody/subheading
                return Path.HasNOrMoreOccurancesOfChar(3, '/');                
            }
        }

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
        public bool HasSemantics(FieldSemantics fieldSemantics)
        {
            return Semantics.Any(s => s.Equals(fieldSemantics));
        }


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
        public override string ToString()
        {
            return string.Format("{0} {1} ({2})", GetType().Name, Name, Path);
        }
    }
}
