using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
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
                    Log.Debug("Generic Topic encountered. Trying to convert to Strongly Typed Topic...");
                    EntityModel stronglyTypedTopic = BuildStronglyTypedTopic(genericTopic);
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

        public EntityModel BuildStronglyTypedTopic(Topic genericTopic)
        {
            using (new Tracer(genericTopic))
            {
                HtmlNode rootElement = null;
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

                Type stronglyTypedTopicType = DetermineStronglyTypedTopicType(rootElement);
                if (stronglyTypedTopicType == null)
                {
                    Log.Debug("No matching Strongly Typed Topic Model found.");
                    return null;
                }
                Log.Debug("Matching Strongly Typed Topic Model found: '{0}'.", stronglyTypedTopicType.FullName);

                EntityModel stronglyTypedTopic = (EntityModel)stronglyTypedTopicType.CreateInstance();
                MapSemanticProperties(stronglyTypedTopic, rootElement);

                return stronglyTypedTopic;
            }
        }

        #region Overridables
        protected virtual HtmlNode ParseHtml(string html)
        {
            using (new Tracer(html))
            {
                HtmlDocument topicHtmlDoc = new HtmlDocument();
                topicHtmlDoc.LoadHtml($"<html>{html}</html>");
                return topicHtmlDoc.DocumentNode.FirstChild;
            }
        }

        protected virtual string GetPropertyXPath(string propertyName)
        {
            return $".//*[contains(@class, '{propertyName} ')]";
        }

        protected virtual Type DetermineStronglyTypedTopicType(HtmlNode rootElement)
        {
            foreach (KeyValuePair<string, Type> kvp in ModelTypeRegistry.GetStronglyTypedTopicModels())
            {
                string xpath = GetPropertyXPath(kvp.Key);
                Log.Debug($"Trying XPath '{xpath}' for type '{kvp.Value.FullName}'");
                if (rootElement.SelectSingleNode(xpath) != null)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        protected virtual void MapSemanticProperties(EntityModel stronglyTypedTopic, HtmlNode rootElement)
        {
            Type modelType = stronglyTypedTopic.GetType();
            IDictionary<string, List<SemanticProperty>> propertySemanticsMap = ModelTypeRegistry.GetPropertySemantics(modelType);
            foreach (KeyValuePair<string, List<SemanticProperty>> propertySemantics in propertySemanticsMap)
            {
                PropertyInfo modelProperty = modelType.GetProperty(propertySemantics.Key);
                List<SemanticProperty> semanticProperties = propertySemantics.Value;
                HtmlNodeCollection htmlElements = null;
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

        protected virtual object GetPropertyValue(Type modelPropertyType, HtmlNodeCollection htmlElements)
        {
            bool isListProperty = modelPropertyType.IsGenericList();
            Type targetType = modelPropertyType.GetUnderlyingGenericListType() ?? modelPropertyType;

            object result;
            switch (targetType.FullName)
            {
                case "System.String":
                    if (isListProperty)
                    {
                        result = htmlElements.Select(e => e.InnerHtml).ToList();
                    }
                    else
                    {
                        result = htmlElements[0].InnerHtml;
                    }
                    break;

                case "Sdl.Web.Common.Models.Link":
                    if (isListProperty)
                    {
                        result = htmlElements.Select(e => GetLink(e)).ToList();
                    }
                    else
                    {
                        result = GetLink(htmlElements[0]);
                    }
                    break;

                default:
                    throw new DxaException($"Unexpected property type '{targetType.FullName}'");
            }

            return result;
        }

        protected virtual Link GetLink(HtmlNode htmlElement)
        {
            HtmlNode hyperlink;
            if (htmlElement.Name == "a")
            {
                hyperlink = htmlElement;
            }
            else
            {
                hyperlink = htmlElement.SelectSingleNode(".//a");
                if (hyperlink == null)
                {
                    Log.Debug($"No hyperlink found in HTML element: {htmlElement.OuterHtml}");
                    return null;
                }
            }

            return new Link
            {
                Url = hyperlink.GetAttributeValue("href", string.Empty),
                LinkText = hyperlink.InnerText,
                AlternateText = hyperlink.GetAttributeValue("title", null)
            };
        }
        #endregion
    }
}
