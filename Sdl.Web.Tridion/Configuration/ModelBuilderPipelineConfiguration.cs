using System.Configuration;

namespace Sdl.Web.Tridion.Configuration
{
    /// <summary>
    /// Represents the configuration section for <see cref="Sdl.Web.Tridion.Mapping.ModelBuilderPipeline"/> configuration.
    /// </summary>
    public class ModelBuilderPipelineConfiguration : ConfigurationSection
    {
        public const string SectionName = "modelBuilderPipeline";
        private const string DefaultCollectionName = "";

        /// <summary>
        /// Gets or sets the collection of configured Model Builders.
        /// </summary>
        [ConfigurationProperty(DefaultCollectionName, IsDefaultCollection = true)]
        public ModelBuilderCollection ModelBuilders
        {
            get
            {
                return this[DefaultCollectionName] as ModelBuilderCollection;
            }
            set
            {
                this[DefaultCollectionName] = value;
            }
        }
    }
}
