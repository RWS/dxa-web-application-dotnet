using System;
using System.Collections.Generic;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for View Models for Entities.
    /// </summary>
    public abstract class EntityModel : ViewModel, IRichTextFragment
    {
        private const string _xpmComponentPresentationMarkup = "<!-- Start Component Presentation: {{\"ComponentID\" : \"{0}\", \"ComponentModified\" : \"{1}\", \"ComponentTemplateID\" : \"{2}\", \"ComponentTemplateModified\" : \"{3}\", \"IsRepositoryPublished\" : {4}}} -->";

        private string _id = string.Empty;

        /// <summary>
        /// Gets or sets the identifier for the Entity.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                _id = value;
            }
        }

        /// <summary>
        /// Gets or sets metadata used to render XPM property markup.
        /// </summary>
        [SemanticProperty(IgnoreMapping = true)]
        public IDictionary<string, string> XpmPropertyMetadata
        {
            get;
            set;
        }

        #region IRichTextFragment Members

        /// <summary>
        /// Gets an HTML representation of the Entity Model.
        /// </summary>
        /// <returns>An HTML representation.</returns>
        /// <remarks>
        /// This method is used when the <see cref="EntityModel"/> is part of a <see cref="RichText"/> instance which is mapped to a string property.
        /// In this case HTML rendering happens during model mapping (by means of this method), which is not ideal.
        /// Preferably, the model property should be of type <see cref="RichText"/> and the View should use @Html.DxaRichText() to get the rich text rendered as HTML.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        /// This method must be overridden in a concrete <see cref="EntityModel"/> subclass which is to be embedded in rich text.
        /// For example, see <see cref="YouTubeVideo.ToHtml"/>.
        /// </exception>
        public virtual string ToHtml()
        {
            throw new NotSupportedException(
                string.Format("Direct rendering of View Model type '{0}' to HTML is not supported." + 
                " Consider using View Model property of type RichText in combination with @Html.DxaRichText() in view code to avoid direct rendering to HTML." +
                " Alternatively, override method {0}.ToHtml().", 
                GetType().FullName)
                );
        }

        #endregion


        #region Overrides

        /// <summary>
        /// Gets the rendered XPM markup
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <returns>The XPM markup.</returns>
        public override string GetXpmMarkup(Localization localization)
        {
            if (XpmMetadata == null)
            {
                return string.Empty;
            }

            // TODO: Consider data-driven approach (i.e. just render all XpmMetadata key/value pairs)
            return string.Format(
                _xpmComponentPresentationMarkup, 
                XpmMetadata["ComponentID"], 
                XpmMetadata["ComponentModified"], 
                XpmMetadata["ComponentTemplateID"], 
                XpmMetadata["ComponentTemplateModified"], 
                XpmMetadata["IsRepositoryPublished"]
                );
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Entity Model.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the specified object in an Entity Model with the same <see cref="Id"/> as the current one.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            EntityModel other = obj as EntityModel;
            if (other == null)
            {
                return false;
            }
            return other.Id == Id;
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current Entity Model.
        /// </returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type and identifier of the Entity.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", GetType().Name, Id);
        }

        #endregion
    }
}
