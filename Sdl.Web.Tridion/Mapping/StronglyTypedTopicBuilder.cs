using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.Common.Models;
using Sdl.Web.DataModel;
using Sdl.Web.Common;
using Sdl.Web.Common.Models.Entity;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Extensions;
using Sdl.Web.Common.Mapping;

namespace Sdl.Web.Tridion.Mapping
{
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
        public void BuildEntityModel(ref EntityModel entityModel, EntityModelData entityModelData, Type baseModelType, ILocalization localization)
        {
            using (new Tracer(entityModel, entityModelData, baseModelType, localization))
            {
                if (entityModel == null)
                {
                    throw new DxaException($"The {GetType().Name} must be configured after the DefaultModelBuilder.");
                }

                Topic genericTopic = entityModel as Topic;
                if (genericTopic != null)
                {
                    Log.Debug("Generic Topic encountered...");
                    EntityModel stronglyTypedTopic = ConvertToStronglyTypedTopic(genericTopic);
                    if (stronglyTypedTopic != null)
                    {
                        Log.Debug($"Converted {genericTopic} to {stronglyTypedTopic}");
                        entityModel = stronglyTypedTopic;
                    }
                    else
                    {
                        Log.Warn($"Unable to convert {genericTopic} to Strongly Typed Topic.");
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Tries to convert a given generic Topic to a Strongly Typed Topic Model.
        /// </summary>
        /// <param name="genericTopic">The generic Topic to convert.</param>
        /// <returns>The Strongly Typed Topic Model or <c>null</c> if the generic Topic cannot be converted.</returns>
        public EntityModel ConvertToStronglyTypedTopic(Topic genericTopic)
        {
            using (new Tracer(genericTopic))
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
                    rootElement = ParseHtml(genericTopic.TopicBody);
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to parse generic Topic HTML.");
                    Log.Error(ex);
                    return null;
                }

                Type topicType = DetermineTopicType(rootElement, registeredTopicTypes);
                if (topicType == null)
                {
                    Log.Debug("No matching Strongly Typed Topic Model found.");
                    return null;
                }

                return BuildStronglyTypedTopic(topicType, rootElement);
            }
        }

        #region Overridables
        protected virtual XmlElement ParseHtml(string xhtml)
        {
            using (new Tracer(xhtml))
            {
                XmlDocument topicXmlDoc = new XmlDocument();
                topicXmlDoc.LoadXml($"<topic>{xhtml}</topic>");
                return topicXmlDoc.DocumentElement;
            }
        }

        protected virtual string GetPropertyXPath(string propertyName)
        {
            if (propertyName == "_Content")
                return ".";

            string[] propertyNameSegments = propertyName.Split('/');
            StringBuilder xPathBuilder = new StringBuilder(".");
            foreach (string propertyNameSegment in propertyNameSegments)
            {
                xPathBuilder.Append($"//*[contains(@class, '{propertyNameSegment} ')]");
            }
            return xPathBuilder.ToString();
        }

        protected virtual Type DetermineTopicType(XmlElement rootElement, IEnumerable<Tuple<string, Type>> registeredTopicTypes)
        {
            Type bestMatch = null;
            int bestMatchClassPos = -1;

            foreach (Tuple<string, Type> tuple in registeredTopicTypes)
            {
                string propertyName = tuple.Item1;
                Type modelType = tuple.Item2;
                string xPath = GetPropertyXPath(propertyName);

                Log.Debug($"Trying XPath '{xPath}' for type '{modelType.FullName}'");
                XmlElement matchedElement = rootElement.SelectSingleNode(xPath) as XmlElement;
                if (matchedElement != null)
                {
                    int classPos = matchedElement.GetAttribute("class").IndexOf(propertyName);
                    if (classPos > bestMatchClassPos)
                    {
                        bestMatch = modelType;
                        bestMatchClassPos = classPos;
                    }
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
                XmlNodeList htmlElements = null;
                foreach (SemanticProperty ditaProperty in semanticProperties.Where(sp => sp.SemanticType.Vocab == ViewModel.DitaVocabulary))
                {
                    string ditaPropertyName = ditaProperty.PropertyName;
                    string propertyXPath = GetPropertyXPath(ditaPropertyName);
                    Log.Debug($"Trying XPath '{propertyXPath}' for property '{modelProperty.Name}'");
                    htmlElements = rootElement.SelectNodes(propertyXPath);
                    if (htmlElements != null && htmlElements.Count > 0)
                    {
                        break;
                    }
                    Log.Debug($"No HTML elements found for DITA property '{ditaPropertyName}'.");
                }
                if (htmlElements == null || htmlElements.Count == 0)
                {
                    Log.Debug($"Unable to map property '{modelProperty.Name}'");
                    continue;
                }
                Log.Debug($"{htmlElements.Count} HTML elements found.");

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

        protected virtual object GetPropertyValue(Type modelPropertyType, XmlNodeList htmlElements)
        {
            bool isListProperty = modelPropertyType.IsGenericList();
            Type targetType = modelPropertyType.GetUnderlyingGenericListType() ?? modelPropertyType;

            object result;
            if (targetType == typeof(string))
            {
                if (isListProperty)
                    result = htmlElements.OfType<XmlElement>().Select(e => e.InnerText).ToList();
                else
                    result = htmlElements[0].InnerText;
            }
            else if (targetType == typeof(RichText))
            {
                if (isListProperty)
                    result = htmlElements.OfType<XmlElement>().Select(e => new RichText(e.InnerXml)).ToList();
                else
                    result = new RichText(htmlElements[0].InnerXml);
            }
            else if (targetType == typeof(Link))
            {
                if (isListProperty)
                    result = htmlElements.OfType<XmlElement>().Select(e => BuildLink(e)).ToList();
                else
                    result = BuildLink(htmlElements[0] as XmlElement);
            }
            else if (typeof(EntityModel).IsAssignableFrom(targetType))
            {
                if (isListProperty)
                    result = htmlElements.OfType<XmlElement>().Select(e => BuildStronglyTypedTopic(targetType, e)).ToList();
                else
                    result = BuildStronglyTypedTopic(targetType, (XmlElement)htmlElements[0]);
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
                    Log.Debug($"No hyperlink found in HTML element: {htmlElement.OuterXml}");
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
