using System.Configuration;

namespace Sdl.Web.DD4T.Configuration
{
    /// <summary>
    /// Represents the configuration section for <see cref="Sdl.Web.DD4T.Mapping.ModelBuilderPipeline"/> configuration.
    /// </summary>
    public class ModelBuilderPipelineConfiguration : ConfigurationSection
    {
        public const string SectionName = "modelBuilderPipeline";
        private const string _defaultCollectionName = "";

        /// <summary>
        /// Gets or sets the collection of configured Model Builders.
        /// </summary>
        [ConfigurationProperty(_defaultCollectionName, IsDefaultCollection = true)]
        public ModelBuilderCollection ModelBuilders
        {
            get
            {
                return this[_defaultCollectionName] as ModelBuilderCollection;
            }
            set
            {
                this[_defaultCollectionName] = value;
            }
        }
    }
}
