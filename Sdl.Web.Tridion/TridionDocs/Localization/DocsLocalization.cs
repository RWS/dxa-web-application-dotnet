using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Tridion.TridionDocs.Localization
{
    /// <summary>
    /// Docs Localization Wrapper
    /// </summary>
    public class DocsLocalization : Common.Configuration.Localization
    {
        public DocsLocalization()
        {
            // Predefined Topic schema that always has an ID of 1 (deployer adds this)
            SemanticSchema schema = new SemanticSchema
            {
                Id = 1,
                RootElement = "Topic",
                Fields = new List<SemanticSchemaField>
                {
                    new SemanticSchemaField
                    {
                        Name = "topicBody",
                        Path = "/Topic/topicBody",
                        IsMultiValue = false,
                        Semantics = new List<FieldSemantics>
                        {
                            new FieldSemantics {Prefix = "tri", Entity = "Topic", Property = "topicBody"},
                            new FieldSemantics {Prefix = "s", Entity = "Topic", Property = "topicBody"},
                        },
                        Fields = new List<SemanticSchemaField>()
                    },
                    new SemanticSchemaField
                    {
                        Name = "topicTitle",
                        Path = "/Topic/topicTitle",
                        IsMultiValue = false,
                        Semantics = new List<FieldSemantics>
                        {
                            new FieldSemantics {Prefix = "tri", Entity = "Topic", Property = "topicTitle"},
                            new FieldSemantics {Prefix = "s", Entity = "Topic", Property = "topicTitle"},
                        },
                        Fields = new List<SemanticSchemaField>()
                    }
                },
                Semantics = new List<SchemaSemantics>
                {
                    new FieldSemantics {Prefix = "tri", Entity = "Topic"},
                    new FieldSemantics {Prefix = "s", Entity = "Topic"},
                }
            };

            List<SemanticVocabulary> vocabs = new List<SemanticVocabulary>
            {
                new SemanticVocabulary { Prefix = "tri", Vocab = "http://www.sdl.com/web/schemas/core" },
                new SemanticVocabulary { Prefix = "s", Vocab = "http://schema.org/" },
            };

            SetSemanticSchemas(new List<SemanticSchema> {schema}, vocabs);
        }

        public override string Path { get; set; } = ""; // content path

        public override string CmUriScheme { get; } = "ish";

        public override bool IsXpmEnabled { get; set; } = false; // no xpm on dd-webapp      

        protected override void Load()
        {
            using (new Tracer(this))
            {
                LastRefresh = DateTime.Now;
            }
        }

        public override IDictionary GetResources(string sectionName = null)
            => new Hashtable(); // no resources so return empty hash to avoid default impl

        public override bool IsStaticContentUrl(string urlPath)
        {
            List<string> mediaPatterns = new List<string>();
            mediaPatterns.Add("^/favicon.ico");
            mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/assets/.*");
            mediaPatterns.Add($"^{Path}/{SiteConfiguration.SystemFolder}/.*\\.json$");
            StaticContentUrlPattern = string.Join("|", mediaPatterns);
            Regex staticContentUrlRegex = new Regex(StaticContentUrlPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return staticContentUrlRegex.IsMatch(urlPath);
        }
    }
}
