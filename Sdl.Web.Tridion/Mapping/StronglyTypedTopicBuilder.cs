using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Mapping;
using System.Collections;
using Sdl.Web.Common.Configuration;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Model Builder used to convert <see cref="GenericTopic"/> Entity Models to Strongly Typed Topic Models.
    /// </summary>
    /// <remarks>
    /// This class has two use cases:
    /// <list type="bullet">
    ///     <item>It can act as an Entity Model Builder which is configured in the <see cref="ModelBuilderPipeline"/>.</item>
    ///     <item>It can be used directly to convert a given <see cref="GenericTopic"/>. See <see cref="TryConvertToStronglyTypedTopic{T}(GenericTopic)"/>.</item>
    /// </list>
    /// </remarks>
    public class StronglyTypedTopicBuilder : IEntityModelBuilder
    {
        #region IEntityModelBuilder members
        /// <summary>
        /// Builds a strongly typed Entity Model based on a given DXA R2 Data Model.
        /// </summary>
        /// <param name="entityModel">The strongly typed Entity Model to build. Is <c>null</c> for the first Entity Model Builder in the pipeline.</param>
        /// <param name="entityModelData">The DXA R2 Data Model.</param>
        /// <param name="baseModelType">The base type for the Entity Model to build.</param>
        /// <param name="localization">The context <see cref="ILocalization"/>.</param>
        public void BuildEntityModel(ref EntityModel entityModel, EntityModelData entityModelData, Type baseModelType, Localization localization)
        {
            using (new Tracer(entityModel, entityModelData, baseModelType, localization))
            {
                if (entityModel == null)
                {
                    throw new DxaException($"The {GetType().Name} must be configured after the DefaultModelBuilder.");
                }

                GenericTopic genericTopic = entityModel as GenericTopic;
                if (genericTopic != null)
                {
                    Log.Debug("Generic Topic encountered...");
                    EntityModel stronglyTypedTopic = TryConvertToStronglyTypedTopic(genericTopic);
                    if (stronglyTypedTopic != null)
                    {
                        Log.Debug($"Converted {genericTopic} to {stronglyTypedTopic}");
                        entityModel = stronglyTypedTopic;
                    }
                    else
                    {
                        Log.Debug($"Unable to convert {genericTopic} to Strongly Typed Topic.");
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Tries to convert a given generic Topic to a Strongly Typed Topic Model.
        /// </summary>
        /// <param name="genericTopic">The generic Topic to convert.</param>
        /// <param name="ofType">The type of the Strongly Typed Topic Model to convert to. If not specified (or <c>null</c>), the type will be determined from the XHTML.</param>
        /// <returns>The Strongly Typed Topic Model or <c>null</c> if the generic Topic cannot be converted.</returns>
        public EntityModel TryConvertToStronglyTypedTopic(GenericTopic genericTopic, Type ofType = null)
        {
            using (new Tracer(genericTopic, ofType))
            {
                Log.Debug($"Trying to convert {genericTopic} to Strongly Typed Topic Model...");

                IEnumerable<Tuple<string, Type>> registeredTopicTypes = ModelTypeRegistry.GetModelTypesForVocabulary(ViewModel.DitaVocabulary);
                if (registeredTopicTypes == null)
                {
                    Log.Debug("No Strongly Typed Topic Models registered.");
                    return null;
                }

                XmlElement rootElement = null;
                try
                {
                    rootElement = ParseXhtml(genericTopic);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to parse generic Topic XHTML.");
                    Log.Debug(genericTopic.TopicBody);
                    Log.Error(ex);
                    return null;
                }

                Type topicType = ofType;
                if (ofType == null)
                {
                    topicType = DetermineTopicType(rootElement, registeredTopicTypes);
                    if (topicType == null)
                    {
                        Log.Debug("No matching Strongly Typed Topic Model found.");
                        return null;
                    }
                }

                EntityModel stronglyTypedTopic = BuildStronglyTypedTopic(topicType, rootElement);

                if (stronglyTypedTopic.Id == null)
                    stronglyTypedTopic.Id = genericTopic.Id;

                return stronglyTypedTopic;
            }
        }

        /// <summary>
        /// Tries to convert a given generic Topic to a Strongly Typed Topic Model.
        /// </summary>
        /// <param name="genericTopic">The generic Topic to convert.</param>
        /// <typeparam name="T">The type of the Strongly Typed Topic Model to convert to.</typeparam>
        /// <returns>The Strongly Typed Topic Model or <c>null</c> if the generic Topic cannot be converted to the given type.</returns>
        public T TryConvertToStronglyTypedTopic<T>(GenericTopic genericTopic) where T: EntityModel
            => (T)TryConvertToStronglyTypedTopic(genericTopic, typeof(T));

        #region Overridables
        protected virtual XmlElement ParseXhtml(GenericTopic genericTopic)
        {
            using (new Tracer(genericTopic))
            {
                XmlDocument topicXmlDoc = new XmlDocument();
                topicXmlDoc.LoadXml($"<topic>{genericTopic.TopicBody}</topic>");

                XmlElement topicElement = topicXmlDoc.DocumentElement;

                // Inject GenericTopic's TopicTitle as additional HTML element
                XmlElement topicTitleElement = topicXmlDoc.CreateElement("h1");
                topicTitleElement.SetAttribute("class", "_topicTitle");
                topicTitleElement.InnerText = genericTopic.TopicTitle;
                topicElement.AppendChild(topicTitleElement);

                return topicElement;
            }
        }

        protected virtual string GetPropertyXPath(string propertyName)
        {
            if (propertyName == SemanticProperty.Self)
                return ".";

            string[] propertyNameSegments = propertyName.Split('/');
            StringBuilder xPathBuilder = new StringBuilder(".");
            foreach (string propertyNameSegment in propertyNameSegments)
            {
                xPathBuilder.Append($"//*[contains(@class, '{propertyNameSegment}')]");
            }
            return xPathBuilder.ToString();
        }

        /// <summary>
        /// Filters the XHTML elements found by the XPath query.
        /// </summary>
        /// <remarks>
        /// Because we use "contains" in the XPath, it may match on part of a class name.
        /// We filter out any partial matches here.
        /// </remarks>
        protected virtual IEnumerable<XmlElement> FilterXPathResults(XmlNodeList htmlElements, string ditaPropertyName)
        {
            if (htmlElements == null || htmlElements.Count == 0)
                return null;

            if (ditaPropertyName == SemanticProperty.Self)
                return htmlElements.Cast<XmlElement>();

            // Only look at last path segment
            int lastSlashPos = ditaPropertyName.LastIndexOf('/');
            if (lastSlashPos >= 0)
                ditaPropertyName = ditaPropertyName.Substring(lastSlashPos + 1);

            IList<XmlElement> result = new List<XmlElement>(htmlElements.Count);
            foreach (XmlElement htmlElement in htmlElements)
            {
                string[] classes = htmlElement.GetAttribute("class").Split(' ');
                if (classes.Contains(ditaPropertyName))
                    result.Add(htmlElement);
            }
            return result;
        }

        protected virtual Type DetermineTopicType(XmlElement rootElement, IEnumerable<Tuple<string, Type>> registeredTopicTypes)
        {
            Type bestMatch = null;
            int bestMatchClassPos = -1;

            foreach (Tuple<string, Type> tuple in registeredTopicTypes)
            {
                string propertyName = tuple.Item1;
                Type modelType = tuple.Item2;

                if (string.IsNullOrEmpty(propertyName))
                {
                    Log.Debug($"Skipping Type '{modelType.FullName}' (no EntityName specified).");
                    continue;
                }

                string xPath = GetPropertyXPath(propertyName);

                Log.Debug($"Trying XPath \"{xPath}\" for type '{modelType.FullName}'");
                XmlElement matchedElement = rootElement.SelectSingleNode(xPath) as XmlElement;
                if (matchedElement != null)
                {
                    Log.Debug("Matching XHTML element found.");
                    int classPos = matchedElement.GetAttribute("class").IndexOf(propertyName);
                    if (classPos > bestMatchClassPos)
                    {
                        bestMatch = modelType;
                        bestMatchClassPos = classPos;
                    }
                }
                else
                {
                    Log.Debug("No matching XHTML element found.");
                }
            }

            return bestMatch;
        }

        protected virtual EntityModel BuildStronglyTypedTopic(Type modelType, XmlElement htmlElement)
        {
            Log.Debug($"Building Strongly Typed Topic Model '{modelType.FullName}'...");
            EntityModel result = (EntityModel)modelType.CreateInstance();
            MapBaseProperties(result, htmlElement);
            MapSemanticProperties(result, htmlElement);

            // Let the View Model determine the View to be used.
            // Do this after mapping all properties so that the View name can be derived from the properties if needed.
            // NOTE: Currently passing in null for Context Localization (not expecting this to be used).
            result.MvcData = result.GetDefaultView(null);

            return result;
        }

        protected virtual void MapBaseProperties(EntityModel stronglyTypedTopic, XmlElement htmlElement)
        {
            stronglyTypedTopic.Id = htmlElement.GetAttributeNode("id")?.Value;
            stronglyTypedTopic.HtmlClasses = htmlElement.GetAttributeNode("class")?.Value;
        }

        protected virtual void MapSemanticProperties(EntityModel stronglyTypedTopic, XmlElement rootElement)
        {
            Type modelType = stronglyTypedTopic.GetType();
            IDictionary<string, List<SemanticProperty>> propertySemanticsMap = ModelTypeRegistry.GetPropertySemantics(modelType);
            foreach (KeyValuePair<string, List<SemanticProperty>> propertySemantics in propertySemanticsMap)
            {
                PropertyInfo modelProperty = modelType.GetProperty(propertySemantics.Key);
                List<SemanticProperty> semanticProperties = propertySemantics.Value;
                IEnumerable<XmlElement> htmlElements = null;
                foreach (SemanticProperty ditaProperty in semanticProperties.Where(sp => sp.SemanticType.Vocab == ViewModel.DitaVocabulary))
                {
                    string ditaPropertyName = ditaProperty.PropertyName;
                    string propertyXPath = GetPropertyXPath(ditaPropertyName);
                    Log.Debug($"Trying XPath \"{propertyXPath}\" for property '{modelProperty.Name}'");
                    XmlNodeList xPathResults = rootElement.SelectNodes(propertyXPath);
                    htmlElements = FilterXPathResults(xPathResults, ditaPropertyName);
                    if (htmlElements != null && htmlElements.Any())
                        break;
                    Log.Debug($"No XHTML elements found for DITA property '{ditaPropertyName}'.");
                }
                if (htmlElements == null || !htmlElements.Any())
                {
                    Log.Debug($"Unable to map property '{modelProperty.Name}'");
                    continue;
                }
                Log.Debug($"{htmlElements.Count()} XHTML elements found.");

                try
                {
                    object propertyValue = GetPropertyValue(modelProperty.PropertyType, htmlElements);
                    modelProperty.SetValue(stronglyTypedTopic, propertyValue);
                }
                catch (Exception ex)
                {
                    throw new DxaException($"Unable to map property {modelType.Name}.{modelProperty.Name}", ex);
                }
            }
        }

        protected virtual object GetPropertyValue(Type modelPropertyType, IEnumerable<XmlElement> htmlElements)
        {
            bool isListProperty = modelPropertyType.IsGenericList();
            Type targetType = modelPropertyType.GetUnderlyingGenericListType() ?? modelPropertyType;

            object result;
            if (targetType == typeof(string))
            {
                if (isListProperty)
                    result = htmlElements.Select(e => e.InnerText).ToList();
                else
                    result = htmlElements.First().InnerText;
            }
            else if (targetType == typeof(RichText))
            {
                if (isListProperty)
                    result = htmlElements.Select(e => new RichText(e.InnerXml)).ToList();
                else
                    result = new RichText(htmlElements.First().InnerXml);
            }
            else if (targetType == typeof(Link))
            {
                if (isListProperty)
                    result = htmlElements.Select(e => BuildLink(e)).ToList();
                else
                    result = BuildLink(htmlElements.First());
            }
            else if (typeof(EntityModel).IsAssignableFrom(targetType))
            {
                if (isListProperty)
                {
                    IList stronglyTypedModels = targetType.CreateGenericList();
                    foreach (XmlElement htmlElement in htmlElements)
                    {
                        stronglyTypedModels.Add(BuildStronglyTypedTopic(targetType, htmlElement));
                    }
                    result = stronglyTypedModels;
                }
                else
                    result = BuildStronglyTypedTopic(targetType, htmlElements.First());
            }
            else
            {
                throw new DxaException($"Unexpected property type '{targetType.FullName}'");
            }
            
            return result;
        }

        protected virtual Link BuildLink(XmlElement htmlElement)
        {
            XmlElement hyperlink;
            if (htmlElement.Name == "a")
            {
                hyperlink = htmlElement;
            }
            else
            {
                hyperlink = htmlElement.SelectSingleNode(".//a") as XmlElement;
                if (hyperlink == null)
                {
                    Log.Debug($"No hyperlink found in XHTML element: {htmlElement.OuterXml}");
                    return null;
                }
            }

            return new Link
            {
                Url = hyperlink.GetAttribute("href"),
                LinkText = hyperlink.InnerText,
                AlternateText = hyperlink.GetAttributeNode("title")?.Value
            };
        }
        #endregion
    }
}
