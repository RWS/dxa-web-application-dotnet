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
            const string topic = "Topic";
            const string topicBody = "topicBody";
            const string topicTitle = "topicTitle";
            const string prefixS = "s";
            const string prefixTri = "tri";

            // Predefined Topic schema that always has an ID of 1 (deployer adds this)
            SemanticSchema schema = new SemanticSchema
            {
                Id = 1,
                RootElement = topic,
                Fields = new List<SemanticSchemaField>
                {
                    new SemanticSchemaField
                    {
                        Name = $"{topicBody}",
                        Path = $"/{topic}/{topicBody}",
                        IsMultiValue = false,
                        Semantics = new List<FieldSemantics>
                        {
                            new FieldSemantics {Prefix = $"{prefixTri}", Entity = $"{topic}", Property = $"{topicBody}"},
                            new FieldSemantics {Prefix = $"{prefixS}", Entity = $"{topic}", Property = $"{topicBody}"},
                        },
                        Fields = new List<SemanticSchemaField>()
                    },
                    new SemanticSchemaField
                    {
                        Name = $"{topicTitle}",
                        Path = $"/{topic}/{topicTitle}",
                        IsMultiValue = false,
                        Semantics = new List<FieldSemantics>
                        {
                            new FieldSemantics {Prefix = $"{prefixTri}", Entity = $"{topic}", Property = $"{topicTitle}"},
                            new FieldSemantics {Prefix = $"{prefixS}", Entity = $"{topic}", Property = $"{topicTitle}"},
                        },
                        Fields = new List<SemanticSchemaField>()
                    }
                },
                Semantics = new List<SchemaSemantics>
                {
                    new FieldSemantics {Prefix = $"{prefixTri}", Entity = $"{topic}"},
                    new FieldSemantics {Prefix = $"{prefixS}", Entity = $"{topic}"},
                }
            };

            List<SemanticVocabulary> vocabs = new List<SemanticVocabulary>
            {
                new SemanticVocabulary { Prefix = $"{prefixTri}", Vocab = "http://www.sdl.com/web/schemas/core" },
                new SemanticVocabulary { Prefix = $"{prefixS}", Vocab = "http://schema.org/" },
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
