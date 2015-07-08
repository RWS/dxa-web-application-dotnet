using System.Configuration;

namespace Sdl.Web.DD4T.Configuration
{
    /// <summary>
    /// Represents the configuration settings for a Model Builder.
    /// </summary>
    public sealed class ModelBuilderSettings : ConfigurationElement
    {
        /// <summary>
        /// Gets or set the (assembly qualified) type name of the Model Builder.
        /// </summary>
        [ConfigurationProperty("type", IsRequired = true)]
        public string Type
        {
            get
            {
                return (string)this["type"];
            }
            set
            {
                this["type"] = value;
            }
        }

    }
}
