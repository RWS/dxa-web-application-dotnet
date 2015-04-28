using System.Reflection;
using Sdl.Web.Common;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Writes out semantic markup for use by machine processing (search engines, XPM etc.)
    /// </summary>
    public static class Markup
    {
        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given Entity.
        /// </summary>
        /// <param name="entity">The Entity.</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        public static MvcHtmlString Entity(IEntity entity)
        {
            StringBuilder markupBuilder = new StringBuilder();

            IDictionary<string, string> prefixMappings;
            string[] semanticTypes = ModelTypeRegistry.GetSemanticTypes(entity.GetType(), out prefixMappings);
            if (semanticTypes.Any())
            {
                markupBuilder.AppendFormat(
                    "prefix=\"{0}\" typeof=\"{1}\"",
                    string.Join(" ", prefixMappings.Select(pm => string.Format("{0}: {1}", pm.Key, pm.Value))), 
                    String.Join(" ", semanticTypes)
                    );
            }

            if (WebRequestContext.IsPreview && (entity.EntityData != null))
            {
                foreach (KeyValuePair<string, string> item in entity.EntityData)
                {
                    if (markupBuilder.Length > 0)
                    {
                        markupBuilder.Append(" ");
                    }
                    // add data- attributes using all lowercase chars, since that is what we look for in ParseComponentPresentation
                    markupBuilder.AppendFormat("data-{0}=\"{1}\"", item.Key.ToLowerInvariant(), HttpUtility.HtmlAttributeEncode(item.Value));
                }
            }

            return new MvcHtmlString(markupBuilder.ToString());
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property of a given Entity.
        /// </summary>
        /// <param name="entity">The Entity which contains the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        public static MvcHtmlString Property(IEntity entity, string propertyName, int index = 0)
        {
            PropertyInfo propertyInfo = entity.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new DxaException(
                    string.Format("Entity Type '{0}' does not have a property named '{1}'.", entity.GetType().Name, propertyName)
                    );
            }
            return Property(entity, propertyInfo, index);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property of a given Entity.
        /// </summary>
        /// <param name="entity">The Entity which contains the property.</param>
        /// <param name="propertyInfo">The reflected property info.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        internal static MvcHtmlString Property(IEntity entity, MemberInfo propertyInfo, int index = 0)
        {
            string markup = string.Empty;
            string propertyName = propertyInfo.Name;

            string[] semanticPropertyNames = ModelTypeRegistry.GetSemanticPropertyNames(propertyInfo.DeclaringType, propertyName);
            if (semanticPropertyNames != null && semanticPropertyNames.Any())
            {
                markup = string.Format("property=\"{0}\"", string.Join(" ", semanticPropertyNames));
            }

            string xpath;
            if (WebRequestContext.IsPreview && entity.PropertyData.TryGetValue(propertyName, out xpath))
            {
                string predicate = xpath.EndsWith("]") ? string.Empty : string.Format("[{0}]", index + 1);
                markup += string.Format(" data-xpath=\"{0}{1}\"", HttpUtility.HtmlAttributeEncode(xpath), predicate);
            }

            return new MvcHtmlString(markup);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given Region.
        /// </summary>
        /// <param name="region">The Region.</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        public static MvcHtmlString Region(IRegion region)
        {
            // TODO: "Region" is not a valid semantic type!
            string markup = string.Format("typeof=\"{0}\" resource=\"{1}\"", "Region", region.Name);

            if (WebRequestContext.IsPreview)
            {
                markup += string.Format(" data-region=\"{0}\"", region.Name);
            }

            return new MvcHtmlString(markup);
        }
    }
}
