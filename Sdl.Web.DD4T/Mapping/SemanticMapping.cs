using System;
using System.Collections.Generic;
using Sdl.Web.Mvc;
using Sdl.Web.Mvc.Mapping;

namespace Sdl.Web.DD4T.Mapping
{
    /// <summary>
    /// General Semantic Mapping Class which reads schema mapping from json files on disk
    /// </summary>
    public static class SemanticMapping
    {
        public static IStaticFileManager StaticFileManager { get; set; }
        public const string VersionRegex = "(v\\d*.\\d*)";
        public const string SystemFolder = "system";
        public const string CoreModuleName = "core";

        private static List<SemanticVocabulary> _semanticVocabularies;
        public static List<SemanticVocabulary> SemanticVocabularies
        {
            get 
            {
                if (_semanticVocabularies == null)
                {
                    LoadMapping();
                }
                return _semanticVocabularies;
            }
        }

        private static Dictionary<string, List<SemanticSchema>> _semanticMap;
        public static Dictionary<string, List<SemanticSchema>> SemanticMap
        {
            get
            {
                if (_semanticMap == null)
                {
                    LoadMapping();
                }
                return _semanticMap;
            }
        }
        private static readonly object MappingLock = new object();

        /// <summary>
        /// Gets a semantic schema by id
        /// </summary>
        /// <param name="id">The schema ID</param>
        /// <param name="module">The module (eg "Search") - if none specified this defaults to "Core"</param>
        /// <returns>The semantic schema matching the id for the given module</returns>
        public static SemanticSchema GetSchema(string id, string module = CoreModuleName)
        {
            return GetSchema(SemanticMap, id, module);
        }


        private static void LoadMapping()
        {
            Load(AppDomain.CurrentDomain.BaseDirectory);
        }

        private static SemanticSchema GetSchema(Dictionary<string, List<SemanticSchema>> mapping, string id, string type)
        {
            Exception ex;
            if (mapping.ContainsKey(type))
            {
                var list = mapping[type];
                foreach (var semanticSchema in list)
                {
                    if (semanticSchema.Id.Equals(id))
                    {
                        return semanticSchema;
                    }
                }
                ex = new Exception(String.Format("Semantic Schema '{0}' for module '{1}' does not exist.", id, type));
            }
            else
            {
                ex = new Exception(String.Format("Semantic mappings for module '{0}' do not exist.", type));
            }

            Log.Error(ex);
            throw ex;
        }

        /// <summary>
        /// Loads semantic mapping into memory from database/disk
        /// </summary>
        /// <param name="applicationRoot">The root filepath of the application</param>
        public static void Load(string applicationRoot)
        {
            // we are updating a static variable, so need to be thread safe
            lock (MappingLock)
            {
                // ensure that the config files have been written to disk
                StaticFileManager.CreateStaticAssets(applicationRoot);

                _semanticMap = new Dictionary<string, List<SemanticSchema>>();
                _semanticVocabularies = new List<SemanticVocabulary>();


            }
        }
    }
}
