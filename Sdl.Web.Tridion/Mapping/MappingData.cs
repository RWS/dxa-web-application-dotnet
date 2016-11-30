using System;
using System.Collections.Generic;
using System.Linq;
using DD4T.ContentModel;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Internal data structure for model mapping purposes.
    /// </summary>
    public class MappingData
    {
        public Type TargetType { get; set; }
        public IFieldSet Content { get; set; }
        public IFieldSet Meta { get; set; }
        public Dictionary<string, KeyValuePair<string, string>> TargetEntitiesByPrefix { get; set; }
        public SemanticSchema SemanticSchema { get; set; }
        public SemanticSchemaField EmbeddedSemanticSchemaField { get; set; }
        public ILookup<string, string> EntityNames { get; set; }
        public string ParentDefaultPrefix { get; set; }
        public int EmbedLevel { get; set; }
        public IComponent SourceEntity { get; set; }
        public string ModelId { get; set;}
        public Localization Localization { get; set; }
        public string ContextXPath { get; set; }

        #region Constructors
        /// <summary>
        /// Initializes a new, empty <see cref="MappingData"/> instance.
        /// </summary>
        public MappingData()
        {
        }

        /// <summary>
        /// Initializes a new <see cref="MappingData"/> instance which is a (shallow) copy of another.
        /// </summary>
        /// <param name="other">The other <see cref="MappingData"/> instance to copy.</param>
        public MappingData(MappingData other)
        {
            TargetType = other.TargetType;
            Content = other.Content;
            Meta = other.Meta;
            TargetEntitiesByPrefix = other.TargetEntitiesByPrefix;
            SemanticSchema = other.SemanticSchema;
            EmbeddedSemanticSchemaField = other.EmbeddedSemanticSchemaField;
            EntityNames = other.EntityNames;
            ParentDefaultPrefix = other.ParentDefaultPrefix;
            EmbedLevel = other.EmbedLevel;
            SourceEntity = other.SourceEntity;
            ModelId = other.ModelId;
            Localization = other.Localization;
            ContextXPath = other.ContextXPath;
        }
        #endregion
    }
}