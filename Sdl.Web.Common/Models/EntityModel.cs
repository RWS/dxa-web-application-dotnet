﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Abstract base class for View Models for Entities.
    /// </summary>
    [Serializable]
#pragma warning disable 618
    public abstract class EntityModel : ViewModel, IRichTextFragment, IEntity
#pragma warning restore 618
    {
        #region IEntity members (obsolete)
        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property XpmMetadata instead.")]
        public Dictionary<string, string> EntityData
        {
            get
            {
                return XpmMetadata as Dictionary<string, string>;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property XpmMetadata instead.");
            }
        }

        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property XpmPropertyMetadata instead.")]
        public Dictionary<string, string> PropertyData
        {
            get
            {
                return XpmPropertyMetadata as Dictionary<string, string>;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property XpmPropertyMetadata instead.");
            }
        }

        [SemanticProperty(IgnoreMapping = true)]
        [JsonIgnore]
        [Obsolete("Deprecated in DXA 1.1. Use property MvcData instead.")]
        public MvcData AppData
        {
            get
            {
                return MvcData;
            }
            set
            {
                throw new NotSupportedException("Setting this property is not supported in DXA 1.1. Use property MvcData instead.");
            }
        }
        #endregion

        /// <summary>
        /// Gets or sets the identifier for the Entity.
        /// </summary>
        /// <remarks>
        /// Note that class <see cref="EntityModel"/> is also used for complex types which are not really Entities and thus don't have an Identifier.
        /// Therefore, <see cref="Id"/> can be <c>null</c>.
        /// </remarks>
        [SemanticProperty(IgnoreMapping = true)]
        public string Id
        {
            get;
            set;
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

        #region Overridables
        /// <summary>
        /// Gets the default View for this Entity Model (if any).
        /// </summary>
        /// <param name="localization">The context Localization.</param>
        /// <remarks>
        /// If this method is overridden in a subclass, it will be possible to render "embedded" Entity Models of that type using the Html.DxaEntity method.
        /// </remarks>
        public virtual MvcData GetDefaultView(Localization localization)
        {
            return null;
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
            return (XpmMetadata == null) ? string.Empty : string.Format("<!-- Start Component Presentation: {0} -->", JsonConvert.SerializeObject(XpmMetadata));
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
            return (Id == null) ? ReferenceEquals(this, other) : (other.Id == Id);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current Entity Model.
        /// </returns>
        public override int GetHashCode()
        {
            return (Id == null) ? base.GetHashCode() : Id.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string containing the type and identifier of the Entity.
        /// </returns>
        public override string ToString()
        {
            return (Id == null) ? GetType().Name : String.Format("{0}: {1}", GetType().Name, Id);
        }

        /// <summary>
        /// Creates a deep copy of this View Model.
        /// </summary>
        /// <returns>The copied View Model.</returns>
        public override ViewModel DeepCopy()
        {
            EntityModel clone = (EntityModel) base.DeepCopy();

            if (XpmPropertyMetadata != null)
            {
                clone.XpmPropertyMetadata = new Dictionary<string, string>(XpmPropertyMetadata);
            }

            return clone;
        }

        #endregion
    }
}
