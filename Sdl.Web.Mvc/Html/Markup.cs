using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Sdl.Web.Common.Models;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Writes out semantic markup for use by machine processing (search engines, XPM etc.)
    /// </summary>
    public static class Markup
    {
        /// <summary>
        /// Mark up an entity
        /// </summary>
        /// <param name="entity">The entity to markup</param>
        /// <returns>semantic markup for the given entity</returns>
        public static MvcHtmlString Entity(IEntity entity)
        {
            StringBuilder data = new StringBuilder();
            var prefixes = new Dictionary<string, string>();
            var entityTypes = new List<string>();
            foreach (SemanticEntityAttribute attribute in entity.GetType().GetCustomAttributes(true).Where(a => a is SemanticEntityAttribute).ToList())
            {
                //We only write out public semantic entities
                if (attribute.Public)
                {
                    var prefix = attribute.Prefix;
                    if (!String.IsNullOrEmpty(prefix))
                    {
                        prefixes.Add(prefix, attribute.Vocab);
                        if (!prefixes.ContainsKey(prefix))
                        {
                            prefixes.Add(prefix, attribute.Vocab);
                        }
                        entityTypes.Add(String.Format("{0}:{1}", prefix, attribute.EntityName));
                    }
                }
            }
            if (prefixes.Count > 0)
            {
                data.AppendFormat("prefix=\"{0}\" typeof=\"{1}\"", String.Join(" ", prefixes.Select(p => String.Format("{0}: {1}", p.Key, p.Value))), String.Join(" ", entityTypes));
            }
            if (WebRequestContext.IsPreview)
            {
                if (entity.EntityData != null)
                {
                    foreach (var item in entity.EntityData)
                    {
                        if (data.Length > 0)
                        {
                            data.Append(" ");
                        }
                        // add data- attributes using all lowercase chars, since that is what we look for in ParseComponentPresentation
                        data.AppendFormat("data-{0}=\"{1}\"", item.Key.ToLowerInvariant(), HttpUtility.HtmlAttributeEncode(item.Value));
                    }
                }
            }
            return new MvcHtmlString(data.ToString());
        }

        /// <summary>
        /// Mark up a property value
        /// </summary>
        /// <param name="entity">The entity which contains the property</param>
        /// <param name="property">The property name</param>
        /// <param name="index">The index of the property value (for multi value properties)</param>
        /// <returns>semantic markup for the property value</returns>
        public static MvcHtmlString Property(IEntity entity, string property, int index = 0)
        {
            StringBuilder data = new StringBuilder();
            var pi = entity.GetType().GetProperty(property);
            if (pi != null)
            {
                var entityTypes = entity.GetType().GetCustomAttributes(true).Where(a => a is SemanticEntityAttribute).ToList();
                var publicPrefixes = new List<string>();
                foreach(SemanticEntityAttribute entityType in entityTypes)
                {
                    if (entityType.Public && !publicPrefixes.Contains(entityType.Prefix))
                    {
                        publicPrefixes.Add(entityType.Prefix);
                    }
                }
                var propertyTypes = new List<string>();
                foreach (SemanticPropertyAttribute propertyType in pi.GetCustomAttributes(true).Where(a => a is SemanticPropertyAttribute))
                {
                    string prefix = propertyType.PropertyName.Contains(":") ? propertyType.PropertyName.Split(':')[0] : "";
                    if (!String.IsNullOrEmpty(prefix) && publicPrefixes.Contains(prefix))
                    {
                        propertyTypes.Add(propertyType.PropertyName);
                    }
                }
                if (propertyTypes.Count > 0)
                {
                    data.AppendFormat("property=\"{0}\"", String.Join(" ", propertyTypes));
                }
                if (WebRequestContext.IsPreview)
                {
                    if (entity.PropertyData.ContainsKey(property))
                    {
                        var xpath = entity.PropertyData[property];
                        var suffix = xpath.EndsWith("]") ? String.Empty : String.Format("[{0}]", index + 1);
                        data.AppendFormat("data-xpath=\"{0}{1}\"", HttpUtility.HtmlAttributeEncode(xpath), suffix);
                    }
                }
            }
            return new MvcHtmlString(data.ToString());
        }

        /// <summary>
        /// Mark up a region
        /// </summary>
        /// <param name="region">The region to mark up</param>
        /// <returns>Semantic markup for a region</returns>
        public static MvcHtmlString Region(IRegion region)
        {
            var data = String.Empty;
            if (WebRequestContext.IsPreview)
            {
                data = String.Format(" data-region=\"{0}\"", region.Name);
            }

            return new MvcHtmlString(String.Format("typeof=\"{0}\" resource=\"{1}\"{2}", "Region", region.Name, data));
        }
    }
}