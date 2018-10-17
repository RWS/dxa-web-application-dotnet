using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;
using Sdl.Web.Common.Extensions;

namespace Sdl.Web.Common.Models
{
    /// <summary>
    /// Represents a Registry of View Model Types and associated Semantics.
    /// </summary>
    public static class ModelTypeRegistry
    {
        private static readonly IDictionary<MvcData, Type> _viewToModelTypeMapping = new Dictionary<MvcData, Type>();
        private static readonly IDictionary<Type, SemanticInfo> _modelTypeToSemanticInfoMapping = new Dictionary<Type, SemanticInfo>();
        private static readonly IDictionary<string, ISet<Type>> _semanticTypeToModelTypesMapping = new Dictionary<string, ISet<Type>>();
        private static readonly IDictionary<string, Type> _stronglyTypedTopicModelMapping = new Dictionary<string, Type>();

        private class SemanticInfo
        {
            internal readonly IDictionary<string, string> PrefixMappings = new Dictionary<string, string>();
            internal readonly IList<string> PublicSemanticTypes = new List<string>();
            internal readonly IList<string> MappedSemanticTypes = new List<string>();
            internal readonly IDictionary<string, IList<string>> SemanticProperties = new Dictionary<string, IList<string>>();
            internal readonly Dictionary<string, List<SemanticProperty>> PropertySemantics = new Dictionary<string, List<SemanticProperty>>();
            internal readonly IDictionary<string, SemanticType> PrefixToSemanticTypeMap = new Dictionary<string, SemanticType>();
        }

        /// <summary>
        /// Registers a View Model and associated View.
        /// </summary>
        /// <param name="viewData">The data for the View to register or <c>null</c> if only the Model Type is to be registered.</param>
        /// <param name="modelType">The model Type used by the View.</param>
        public static void RegisterViewModel(MvcData viewData, Type modelType)
        {
            using (new Tracer(viewData, modelType))
            {
                lock (_viewToModelTypeMapping)
                {
                    if (viewData != null)
                    {
                        if (_viewToModelTypeMapping.ContainsKey(viewData))
                        {
                            Log.Warn("View '{0}' registered multiple times.", viewData);
                            return;
                        }
                        _viewToModelTypeMapping.Add(viewData, modelType);
                    }

                    // Obtain a lock on _modelTypeToSemanticInfoMapping too to prevent a race with GetSemanticInfo
                    lock (_modelTypeToSemanticInfoMapping)
                    {
                        if (!_modelTypeToSemanticInfoMapping.ContainsKey(modelType))
                        {
                            RegisterModelType(modelType);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Registers a View Model mapping by compiling a given view file and obtaining its model type.
        /// </summary>
        /// <param name="viewData">The data for the View to register.</param>
        /// <param name="viewVirtualPath">The (virtual) path to the View file.</param>
        public static void RegisterViewModel(MvcData viewData, string viewVirtualPath)
        {
            using (new Tracer(viewData, viewVirtualPath))
            {
                lock (_viewToModelTypeMapping)
                {
                    if (_viewToModelTypeMapping.ContainsKey(viewData))
                    {
                        Log.Warn("View '{0}' registered multiple times. Virtual Path: '{1}'", viewData, viewVirtualPath);
                        return;
                    }

                    try
                    {
                        Type compiledViewType = BuildManager.GetCompiledType(viewVirtualPath);
                        if (!compiledViewType.BaseType.IsGenericType)
                        {
                            throw new DxaException("View is not strongly typed. Please ensure you use the @model directive.");
                        }
                        RegisterViewModel(viewData, compiledViewType.BaseType.GetGenericArguments()[0]);
                    }
                    catch (Exception ex)
                    {
                        throw new DxaException(String.Format("Error occurred while compiling View '{0}'", viewVirtualPath), ex);
                    }
                }
            }
        }

        /// <summary>
        /// Get the View Model Type for a given View.
        /// </summary>
        /// <param name="viewData">The data for the View.</param>
        /// <returns>The View Model Type.</returns>
        public static Type GetViewModelType(MvcData viewData)
        {
            Type modelType;
            MvcData bareMvcData = new MvcData
            {
                AreaName = viewData.AreaName,
                ControllerName = viewData.ControllerName,
                ViewName = viewData.ViewName
            };
            if (!_viewToModelTypeMapping.TryGetValue(bareMvcData, out modelType))
            {
                throw new DxaException(
                    String.Format("No View Model registered for View '{0}'. Check that you have registered this View in the '{1}' area registration.", viewData, viewData.AreaName)
                    );
            }
            return modelType;
        }

        /// <summary>
        /// Gets the semantic types (and prefix mappings) for a given Model Type.
        /// </summary>
        /// <param name="modelType">The Model Type.</param>
        /// <param name="prefixMappings">The prefix mappings for the prefixes used by the types.</param>
        /// <returns>The semantic types.</returns>
        public static string[] GetSemanticTypes(Type modelType, out IDictionary<string, string> prefixMappings)
        {
            // No Tracer here to reduce trace noise.
            SemanticInfo semanticInfo = GetSemanticInfo(modelType);
            prefixMappings = semanticInfo.PrefixMappings;
            return semanticInfo.PublicSemanticTypes.ToArray();
        }

        /// <summary>
        /// Gets the semantic property names for a given Model Type and property name.
        /// </summary>
        /// <param name="modelType">The Model Type.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The semantic property names or <c>null</c> if no semantic property names have been registered for the given property.</returns>
        public static string[] GetSemanticPropertyNames(Type modelType, string propertyName)
        {
            // No Tracer here to reduce trace noise.
            SemanticInfo semanticInfo = GetSemanticInfo(modelType);

            IList<string> semanticPropertyNames;
            if (semanticInfo.SemanticProperties.TryGetValue(propertyName, out semanticPropertyNames))
            {
                return semanticPropertyNames.ToArray();
            }
            return null;
        }

        /// <summary>
        /// Gets the Model Types mapped to a given semantic type name.
        /// </summary>
        /// <param name="semanticTypeName">The semantic type name qualified with vocabulary ID.</param>
        /// <returns>The mapped model types or <c>null</c> if no Model types are registered for the given semantic type name.</returns>
        public static IEnumerable<Type> GetMappedModelTypes(string semanticTypeName)
        {
            ISet<Type> mappedModelTypes;
            _semanticTypeToModelTypesMapping.TryGetValue(semanticTypeName, out mappedModelTypes);
            return mappedModelTypes;
        }

        /// <summary>
        /// Gets all registered Strongly Typed Topic Models and their Semantic Entity Names (as keys of the dictionary).
        /// </summary>
        /// <returns></returns>
        public static IDictionary<string, Type> GetStronglyTypedTopicModels()
        {
            return _stronglyTypedTopicModelMapping;
        }

        /// <summary>
        /// Gets a mapping from property names to associated Semantic Properties for a given Model Type.
        /// </summary>
        /// <param name="modelType">The Model Type.</param>
        /// <returns>A a mapping from property names to associated Semantic Properties.</returns>
        public static Dictionary<string, List<SemanticProperty>> GetPropertySemantics(Type modelType)
            => GetSemanticInfo(modelType).PropertySemantics;

        private static SemanticInfo RegisterModelType(Type modelType)
        {
            using (new Tracer(modelType))
            {
                SemanticInfo semanticInfo = ExtractSemanticInfo(modelType);
                _modelTypeToSemanticInfoMapping.Add(modelType, semanticInfo);

                foreach (string semanticTypeName in semanticInfo.MappedSemanticTypes)
                {
                    ISet<Type> mappedModelTypes;
                    if (!_semanticTypeToModelTypesMapping.TryGetValue(semanticTypeName, out mappedModelTypes))
                    {
                        mappedModelTypes = new HashSet<Type>();
                        _semanticTypeToModelTypesMapping.Add(semanticTypeName, mappedModelTypes);
                    }
                    mappedModelTypes.Add(modelType);

                    string[] semanticTypeNameParts = semanticTypeName.Split(':');
                    if (semanticTypeNameParts[0] == ViewModel.DitaVocabulary)
                    {
                        Log.Debug($"Registered Strongly Typed Topic Model: '{semanticTypeNameParts[1]}' -> '{modelType.FullName}'");
                        _stronglyTypedTopicModelMapping.Add(semanticTypeNameParts[1], modelType);
                    }
                }

                if (Log.IsDebugEnabled)
                {
                    if (semanticInfo.PublicSemanticTypes.Any())
                    {
                        Log.Debug("Model type '{0}' has semantic type(s) '{1}'.", modelType.FullName, String.Join(" ", semanticInfo.PublicSemanticTypes));
                        foreach (KeyValuePair<string, IList<string>> kvp in semanticInfo.SemanticProperties)
                        {
                            Log.Debug("\tRegistered property '{0}' as semantic property '{1}'", kvp.Key, String.Join(" ", kvp.Value));
                        }
                    }
                }

                return semanticInfo;
            }
        }

        private static SemanticInfo GetSemanticInfo(Type modelType)
        {
            SemanticInfo semanticInfo;
            if (!_modelTypeToSemanticInfoMapping.TryGetValue(modelType, out semanticInfo))
            {
                // To prevent excessive locking, we only obtain a lock if no semantic info is found.
                // In a race condition, it may get set just before we obtain the lock, so therefore we check once more.
                lock (_modelTypeToSemanticInfoMapping)
                {
                    if (!_modelTypeToSemanticInfoMapping.TryGetValue(modelType, out semanticInfo))
                    {
                        // Just-In-Time model type registration.
                        semanticInfo = RegisterModelType(modelType);
                    }
                }
            }
            return semanticInfo;
        }

        private static SemanticInfo ExtractSemanticInfo(Type modelType)
        {           
            SemanticInfo semanticInfo = new SemanticInfo();

            // Get model type name
            string modelTypeName = modelType.BareTypeName();

            // Built-in semantic type mapping
            semanticInfo.MappedSemanticTypes.Add(SemanticMapping.GetQualifiedTypeName(modelTypeName));

            // Extract semantic info from SemanticEntity attributes on the Model Type.
            foreach (SemanticEntityAttribute attribute in modelType.GetCustomAttributes<SemanticEntityAttribute>(inherit: true))
            {
                semanticInfo.MappedSemanticTypes.Add(SemanticMapping.GetQualifiedTypeName(attribute.EntityName, attribute.Vocab));

                string prefix = attribute.Prefix ?? string.Empty;
                if (attribute.Public && !String.IsNullOrEmpty(attribute.Prefix))
                {
                    string registeredVocab;
                    if (semanticInfo.PrefixMappings.TryGetValue(prefix, out registeredVocab))
                    {
                        // Prefix mapping already exists; must match.
                        if (attribute.Vocab != registeredVocab)
                        {
                            throw new DxaException(
                                String.Format("Attempt to use semantic prefix '{0}' for vocabulary '{1}', but is is already used for vocabulary '{2}",
                                    prefix, attribute.Vocab, registeredVocab)
                                );
                        }
                    }
                    else
                    {
                        semanticInfo.PrefixMappings.Add(prefix, attribute.Vocab);
                    }
                    semanticInfo.PublicSemanticTypes.Add(String.Format("{0}:{1}", prefix, attribute.EntityName));
                }

                // There may be multiple Semantic Entity attributes for the same prefix. The first one will be used.
                if (semanticInfo.PrefixToSemanticTypeMap.ContainsKey(prefix))
                {
                    if(Log.IsDebugEnabled)
                        Log.Debug($"Type '{modelType.FullName}' has multiple SemanticEntity attributes for prefix '{prefix}'. Ignoring '{attribute.EntityName}'.");
                }
                else
                {
                    semanticInfo.PrefixToSemanticTypeMap.Add(prefix, new SemanticType(attribute.EntityName, attribute.Vocab));
                }
            }

            if (!semanticInfo.PrefixToSemanticTypeMap.ContainsKey(string.Empty))
            {
                // If there is no SemanticEntity attribute without prefix, we add an implicit one:
                semanticInfo.PrefixToSemanticTypeMap.Add(string.Empty, new SemanticType(modelTypeName, SemanticMapping.DefaultVocabulary));
            }

            string defaultPrefix;
            bool mapAllProperties;
            SemanticDefaultsAttribute semanticDefaultsAttr = modelType.GetCustomAttribute<SemanticDefaultsAttribute>();
            if (semanticDefaultsAttr == null)
            {
                defaultPrefix = string.Empty;
                mapAllProperties = true;
            }
            else
            {
                defaultPrefix = semanticDefaultsAttr.Prefix;
                mapAllProperties = semanticDefaultsAttr.MapAllProperties;
            }

            foreach (PropertyInfo propertyInfo in modelType.GetProperties())
            {
                // Extract semantic info from SemanticEntity attributes on the Model Type's properties
                bool useImplicitMapping = mapAllProperties;

                List<SemanticPropertyAttribute> attributes = propertyInfo.GetCustomAttributes<SemanticPropertyAttribute>(inherit: true).ToList();

                // check if we should be ignoring this mapping completely
                if (attributes.Where(x => x.IgnoreMapping).Select(x => x).FirstOrDefault() != null)
                    continue;

                List<SemanticProperty> semanticProperties = new List<SemanticProperty>();
                foreach (SemanticPropertyAttribute attribute in attributes)
                {
                    if (string.IsNullOrEmpty(attribute.PropertyName))
                        continue;

                    // check for known property names
                    switch (attribute.PropertyName)
                    {
                        // To do : we need to make this more generic to collect all fields of a given type (e.g. [SemtanticProperty("_all", typeof(Keyword)])
                        case SemanticProperty.AllFields:
                            if (!typeof(IDictionary<string, string>).IsAssignableFrom(propertyInfo.PropertyType) 
                                && !typeof(IDictionary<string, KeywordModel>).IsAssignableFrom(propertyInfo.PropertyType))
                            {
                                    throw new DxaException(
                                    $"Invalid semantics for property {modelType.Name}.{propertyInfo.Name}. Properties with [SemanticProperty(\"_all\")] annotation must be of type Dictionary<string, string> or Dictionary<string, KeywordModel>."
                                    );
                            }
                            break;

                        case SemanticProperty.Self:
                            Type elementType = GetElementType(propertyInfo.PropertyType);
                            if (!typeof(MediaItem).IsAssignableFrom(elementType) && !typeof(Link).IsAssignableFrom(elementType) && (elementType != typeof(string)))
                            {
                                throw new DxaException(
                                    $"Invalid semantics for property {modelType.Name}.{propertyInfo.Name}. Properties with [SemanticProperty(\"_self\")] annotation must be of type MediaItem, Link or String.");
                            }
                            break;
                    }

                    string prefix = attribute.Prefix;
                    string name = attribute.PropertyName;
                    if(prefix!=null)
                    {                    
                        if (semanticInfo.PrefixMappings.ContainsKey(prefix))
                        {
                            IList<string> semanticPropertyNames;
                            if (!semanticInfo.SemanticProperties.TryGetValue(propertyInfo.Name, out semanticPropertyNames))
                            {
                                semanticPropertyNames = new List<string>();
                                semanticInfo.SemanticProperties.Add(propertyInfo.Name, semanticPropertyNames);
                            }
                            semanticPropertyNames.Add(attribute.PropertyName);
                        }
                    }
                    else
                    {
                        // Skip property names without prefix.
                        prefix = defaultPrefix;
                        useImplicitMapping = false;
                    }

                    SemanticType semanticType;
                    if (!semanticInfo.PrefixToSemanticTypeMap.TryGetValue(prefix, out semanticType))
                    {
                        throw new DxaException($"Use of undeclared prefix '{prefix}' in property '{propertyInfo.Name}' in type '{modelType.FullName}'.");
                    }
                    semanticProperties.Add(new SemanticProperty(prefix, name, semanticType));
                }

                if (useImplicitMapping)
                {
                    SemanticType semanticType;
                    if (!semanticInfo.PrefixToSemanticTypeMap.TryGetValue(defaultPrefix, out semanticType))
                    {
                        throw new DxaException($"Use of undeclared prefix '{defaultPrefix}' in property '{propertyInfo.Name}' in type '{modelType.FullName}'.");
                    }
                    SemanticProperty implicitSemanticProperty = new SemanticProperty(
                        String.Empty, GetDefaultSemanticPropertyName(propertyInfo),
                        semanticType
                        );

                    semanticProperties.Add(implicitSemanticProperty);
                }

                if (semanticProperties.Count > 0)
                {
                    if (semanticInfo.PropertySemantics.ContainsKey(propertyInfo.Name))
                    {
                        // Properties with same name can exist is a property is reintroduced with a different signature in a subclass.
                        if(Log.IsDebugEnabled)
                            Log.Debug("Property with name '{0}' is declared multiple times in type {1}.", propertyInfo.Name, modelType.FullName);
                    }
                    else
                    {
                        semanticInfo.PropertySemantics.Add(propertyInfo.Name, semanticProperties);
                    }
                }
            }

            return semanticInfo;
        }

        private static Type GetElementType(Type propertyType)
        {
            return propertyType.GetUnderlyingGenericListType() ?? propertyType;
        }

        private static string GetDefaultSemanticPropertyName(PropertyInfo property)
        {
            // Transform Pascal case into camel case.
            string semanticPropertyName = property.Name.ToCamelCase();
            if (property.PropertyType.IsGenericList() && semanticPropertyName.EndsWith("s"))
            {
                // Remove trailing 's' of List property name
                semanticPropertyName = semanticPropertyName.Substring(0, semanticPropertyName.Length - 1);
            }
            return semanticPropertyName;
        }
    }
}
