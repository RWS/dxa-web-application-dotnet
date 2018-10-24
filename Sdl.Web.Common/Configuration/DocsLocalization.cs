using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Common.Configuration
{
    /// <summary>
    /// Represents a Localization for Tridion Docs content.
    /// </summary>
    /// <remarks>
    /// Much of the Localization configuration is not applicable for Tridion Docs or at least not being published from Tridion Docs Content Manager.
    /// Therefore this class overrides the default <see cref="Localization"/> class.
    /// </remarks>
    public class DocsLocalization : Localization
    {
        /// <summary>
        /// Initializes a new <see cref="DocLocalization"/> instance.
        /// </summary>
        /// <remarks>
        /// When using this constructor, the <see cref="Id"/> property should be set manually.
        /// </remarks>
        public DocsLocalization()
        {
            Path = string.Empty;
            IsXpmEnabled = false;
        }

        /// <summary>
        /// Initializes an new <see cref="DocsLocalization"/> instance for a given Publication identifier.
        /// </summary>
        /// <param name="publicationId">The Tridion Docs Publication Identifier.</param>
        public DocsLocalization(uint publicationId) : this()
        {
            Id = publicationId.ToString();
        }

        /// <summary>
        /// Gets the URI scheme used for CM URIs.
        /// </summary>
        /// <remarks>
        /// Is always "ish" for Tridion Docs Localizations.
        /// </remarks>
        public override string CmUriScheme { get; } = "ish";

        /// <summary>
        /// Loads the Localization's configuration data.
        /// </summary>
        /// <remarks>
        /// Whereas for Tridion Sites, most configuration data is published from the Content Manager, for Tridion Docs it is either not applicable or hard-coded here.
        /// For that reason, this method is overridden and does not call its base implementation.
        /// </remarks>
        protected override void Load()
        {
            using (new Tracer())
            {
                const string rootElementName = "Topic";
                const string topicBodyFieldName = "topicBody";
                const string topicTitleFieldName = "topicTitle";
                const string coreVocabularyPrefix = "tri";

                LastRefresh = DateTime.Now;

                // Predefined Topic schema that always has an ID of 1 (deployer adds this)
                SemanticSchema schema = new SemanticSchema
                {
                    Id = 1,
                    RootElement = rootElementName,
                    Fields = new List<SemanticSchemaField>
                    {
                        new SemanticSchemaField
                        {
                            Name = topicBodyFieldName,
                            Path = $"/{rootElementName}/{topicBodyFieldName}",
                            IsMultiValue = false,
                            Semantics = new List<FieldSemantics>
                            {
                                new FieldSemantics {Prefix = coreVocabularyPrefix, Entity = rootElementName, Property = topicBodyFieldName}
                            },
                            Fields = new List<SemanticSchemaField>()
                        },
                        new SemanticSchemaField
                        {
                            Name = topicTitleFieldName,
                            Path = $"/{rootElementName}/{topicTitleFieldName}",
                            IsMultiValue = false,
                            Semantics = new List<FieldSemantics>
                            {
                                new FieldSemantics {Prefix = coreVocabularyPrefix, Entity = rootElementName, Property = topicTitleFieldName}
                            },
                            Fields = new List<SemanticSchemaField>()
                        }
                    },
                    Semantics = new List<SchemaSemantics>
                    {
                        new FieldSemantics {Prefix = coreVocabularyPrefix, Entity = rootElementName}
                    }
                };

                List<SemanticVocabulary> vocabs = new List<SemanticVocabulary>
                {
                    new SemanticVocabulary { Prefix = coreVocabularyPrefix, Vocab = Models.ViewModel.CoreVocabulary }
                };

                SetSemanticSchemas(new List<SemanticSchema> { schema }, vocabs);

                // TODO: does this make sense for Docs?
                List<string> mediaPatterns = new List<string>();
                mediaPatterns.Add("^/favicon.ico");
                mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/assets/.*");
                mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/.*\\.json$");
                StaticContentUrlPattern = string.Join("|", mediaPatterns);
                _staticContentUrlRegex = new Regex(StaticContentUrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            }
        }

        public override IDictionary GetResources(string sectionName = null)
            => new Hashtable(); // no resources so return empty hash to avoid default impl
    }
}
