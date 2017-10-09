using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using Sdl.Web.Common;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Mvc.Configuration;

namespace Sdl.Web.Mvc.Html
{
    /// <summary>
    /// Utility class for methods which generate semantic markup (HTML/RDFa attributes) for use by machine processing (search engines, XPM etc.)
    /// </summary>
    public static class Markup
    {
        private static readonly IList<IMarkupDecorator> _markupDecorators = new List<IMarkupDecorator>();

        private const string XpmMarkupHtmlAttrName = "data-xpm";
        private const string XpmMarkupXPath = "//*[@" + XpmMarkupHtmlAttrName + "]";
        private const string XpmFieldMarkup = "<!-- Start Component Field: {{\"XPath\":\"{0}\"}} -->";

        private class XpmMarkupMap : Dictionary<string, string>
        {
            private int _index;

            internal string AddXpmMarkup(string xpmMarkup)
            {
                string index = Convert.ToString(_index++);
                Add(index, xpmMarkup);
                return index;
            }

            internal static XpmMarkupMap Current 
            {
                get
                {
                    const string httpContextItemName = "XpmMarkupMap";
                    HttpContext httpContext = HttpContext.Current;
                    XpmMarkupMap result = httpContext.Items[httpContextItemName] as XpmMarkupMap;
                    if (result == null)
                    {
                        result = new XpmMarkupMap();
                        httpContext.Items[httpContextItemName] = result;
                    }
                    return result;
                }
            }
        }

        /// <summary>
        /// Registers a <see cref="IMarkupDecorator"/> implementation.
        /// </summary>
        /// <param name="markupDecoratorType">The type of the markup decorator. The type must have a parameterless constructor and implement <see cref="IMarkupDecorator"/>.</param>
        /// <seealso cref="BaseAreaRegistration.RegisterMarkupDecorator"/>
        internal static void RegisterMarkupDecorator(Type markupDecoratorType)
        {
            using (new Tracer(markupDecoratorType))
            {
                IMarkupDecorator markupDecorator = (IMarkupDecorator) markupDecoratorType.CreateInstance();
                _markupDecorators.Add(markupDecorator);
            }
        }

        /// <summary>
        /// Decorates HTML markup if any markup decorators have been registered.
        /// </summary>
        /// <param name="htmlToDecorate">The HTML to decorate.</param>
        /// <param name="viewModel">The <see cref="ViewModel"/> associated with the HTML fragment.</param>
        /// <returns>The decorated HTML.</returns>
        /// <seealso cref="RegisterMarkupDecorator"/>
        internal static MvcHtmlString DecorateMarkup(MvcHtmlString htmlToDecorate, ViewModel viewModel)
        {
            if (!_markupDecorators.Any())
            {
                // No decorators; nothing to do.
                return htmlToDecorate;
            }

            string htmlString = htmlToDecorate.ToString();
            using (new Tracer(htmlString.Replace("{", string.Empty), viewModel))
            {
                foreach (IMarkupDecorator markupDecorator in _markupDecorators)
                {
                    htmlString = markupDecorator.DecorateMarkup(htmlString, viewModel);
                }

                return new MvcHtmlString(htmlString);
            }
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given Entity Model.
        /// </summary>
        /// <param name="entityModel">The Entity Model.</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        internal static MvcHtmlString RenderEntityAttributes(EntityModel entityModel)
        {
            string markup = string.Empty;

            IDictionary<string, string> prefixMappings;
            string[] semanticTypes = ModelTypeRegistry.GetSemanticTypes(entityModel.GetType(), out prefixMappings);
            if (semanticTypes.Any())
            {
                markup =
                    $"prefix=\"{string.Join(" ", prefixMappings.Select(pm => $"{pm.Key}: {pm.Value}"))}\" typeof=\"{string.Join(" ", semanticTypes)}\"";
            }

            if (WebRequestContext.IsPreview)
            {
                string xpmMarkupAttr = RenderXpmMarkupAttribute(entityModel);
                if (string.IsNullOrEmpty(markup))
                {
                    markup = xpmMarkupAttr;
                }
                else
                {
                    markup += " " + xpmMarkupAttr;
                }
            }

            return new MvcHtmlString(markup);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property of a given Entity Model.
        /// </summary>
        /// <param name="entityModel">The Entity Model which contains the property.</param>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        internal static MvcHtmlString RenderPropertyAttributes(EntityModel entityModel, string propertyName, int index = 0)
        {
            PropertyInfo propertyInfo = entityModel.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new DxaException(
                    $"Entity Type '{entityModel.GetType().Name}' does not have a property named '{propertyName}'."
                    );
            }
            return RenderPropertyAttributes(entityModel, propertyInfo, index);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given property of a given Entity Model.
        /// </summary>
        /// <param name="entityModel">The Entity Model which contains the property.</param>
        /// <param name="propertyInfo">The reflected property info.</param>
        /// <param name="index">The index of the property value (for multi-value properties).</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        internal static MvcHtmlString RenderPropertyAttributes(EntityModel entityModel, MemberInfo propertyInfo, int index = 0)
        {
            string markup = string.Empty;
            string propertyName = propertyInfo.Name;

            string[] semanticPropertyNames = ModelTypeRegistry.GetSemanticPropertyNames(propertyInfo.DeclaringType, propertyName);
            if (semanticPropertyNames != null && semanticPropertyNames.Any())
            {
                markup = $"property=\"{string.Join(" ", semanticPropertyNames)}\"";
            }

            if (WebRequestContext.IsPreview)
            {
                string xpmMarkupAttr = RenderXpmMarkupAttribute(entityModel, propertyName, index);
                if (string.IsNullOrEmpty(markup))
                {
                    markup = xpmMarkupAttr;
                }
                else
                {
                    markup += " " + xpmMarkupAttr;
                }
            }

            return new MvcHtmlString(markup);
        }

        /// <summary>
        /// Generates semantic markup (HTML/RDFa attributes) for a given Region Model.
        /// </summary>
        /// <param name="regionModel">The Region Model.</param>
        /// <returns>The semantic markup (HTML/RDFa attributes).</returns>
        internal static MvcHtmlString RenderRegionAttributes(RegionModel regionModel)
        {
            // TODO: "Region" is not a valid semantic type!
            string markup = $"typeof=\"{"Region"}\" resource=\"{regionModel.Name}\"";

            if (WebRequestContext.IsPreview)
            {
                markup += " " + RenderXpmMarkupAttribute(regionModel);
            }

            return new MvcHtmlString(markup);
        }

        /// <summary>
        /// Renders a temporary HTML attribute containing the XPM markup for a given View Model or property.
        /// </summary>
        /// <seealso cref="TransformXpmMarkupAttributes"/>
        private static string RenderXpmMarkupAttribute(ViewModel viewModel, string propertyName = null, int index = 0)
        {
            string xpmMarkup;
            if (propertyName == null)
            {
                // Region/Entity markup
                xpmMarkup = viewModel.GetXpmMarkup(WebRequestContext.Localization);
                if (string.IsNullOrEmpty(xpmMarkup))
                {
                    return string.Empty;
                }
            }
            else
            {
                // Property markup
                EntityModel entityModel = (EntityModel) viewModel;
                string xpath;
                if (entityModel.XpmPropertyMetadata != null && entityModel.XpmPropertyMetadata.TryGetValue(propertyName, out xpath))
                {
                    string predicate = xpath.EndsWith("]") ? string.Empty : $"[{index + 1}]";
                    xpmMarkup = string.Format(XpmFieldMarkup, HttpUtility.HtmlAttributeEncode(xpath + predicate));
                }
                else
                {
                    return string.Empty;
                }
            }

            // Instead of jamming the entire XPM markup in an HTML attribute, we only put in a reference to the XPM markup.
            string xpmMarkupRef = XpmMarkupMap.Current.AddXpmMarkup(xpmMarkup);
            return $"{XpmMarkupHtmlAttrName}=\"{xpmMarkupRef}\"";
        }

        /// <summary>
        /// Transforms XPM markup contained in HTML attributes to HTML comments inside the HTML elements.
        /// </summary>
        /// <param name="htmlFragment">The HTML fragment to tranform.</param>
        /// <returns>The transformed HTML fragment.</returns>
        internal static string TransformXpmMarkupAttributes(string htmlFragment)
        {
            //HTML Agility pack drops closing option tags for some reason (bug?)
            HtmlNode.ElementsFlags.Remove("option");

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml($"<html>{htmlFragment}</html>");
            HtmlNode rootElement = htmlDoc.DocumentNode.FirstChild;
            HtmlNodeCollection elementsWithXpmMarkup = rootElement.SelectNodes(XpmMarkupXPath);
            if (elementsWithXpmMarkup != null)
            {
                XpmMarkupMap xpmMarkupMap = XpmMarkupMap.Current;
                foreach (HtmlNode elementWithXpmMarkup in elementsWithXpmMarkup)
                {
                    string xpmMarkupRef = ReadAndRemoveAttribute(elementWithXpmMarkup, XpmMarkupHtmlAttrName);
                    string xpmMarkup = xpmMarkupMap[xpmMarkupRef];

                    if (string.IsNullOrEmpty(xpmMarkup))
                    {
                        continue;
                    }

                    HtmlCommentNode xpmMarkupNode = htmlDoc.CreateComment(xpmMarkup);
                    elementWithXpmMarkup.ChildNodes.Insert(0, xpmMarkupNode);

                }
            }
            return rootElement.InnerHtml;
        }

        private static string ReadAndRemoveAttribute(HtmlNode htmlElement, string attributeName)
        {
            if (!htmlElement.Attributes.Contains(attributeName))
            {
                return string.Empty;
            }

            HtmlAttribute attr = htmlElement.Attributes[attributeName];
            htmlElement.Attributes.Remove(attr);
            return HttpUtility.HtmlDecode(attr.Value);
        }
    }
}
