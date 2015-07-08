using System;
using System.Configuration;

namespace Sdl.Web.Tridion.Configuration
{
    /// <summary>
    /// Represents a collection of configured Model Builders.
    /// </summary>
    public sealed class ModelBuilderCollection : ConfigurationElementCollection
    {
        /// <summary>
        /// Adds an element to the collection.
        /// </summary>
        /// <param name="modelBuilderSettings">The element to add.</param>
        public void Add(ModelBuilderSettings modelBuilderSettings)
        {
            BaseAdd(modelBuilderSettings);
        }

        /// <summary>
        /// Removes an element from the collection.
        /// </summary>
        /// <param name="modelBuilderSettings">The element to remove.</param>
        public void Remove(ModelBuilderSettings modelBuilderSettings)
        {
            int index = BaseIndexOf(modelBuilderSettings);

            if (index >= 0)
            {
                BaseRemoveAt(index);
            }
        }

        #region Overrides
        /// <summary>
        /// Gets the type of collection: add/remove/clear map.
        /// </summary>
        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ModelBuilderSettings();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((ModelBuilderSettings)element).Type;
        }

        protected override void BaseAdd(ConfigurationElement resource)
        {
            BaseAdd(resource, false);
        }
        #endregion
    }
}
