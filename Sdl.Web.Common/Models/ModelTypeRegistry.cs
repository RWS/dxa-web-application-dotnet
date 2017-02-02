using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Mapping;

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
        private static readonly Dictionary<Type, Dictionary<string, List<SemanticProperty>>> _modelTypeToPropertySemanticsMapping =
            new Dictionary<Type, Dictionary<string, List<SemanticProperty>>>();


        private class SemanticInfo
        {
            internal readonly IDictionary<string, string> PrefixMappings = new Dictionary<string, string>();
            internal readonly IList<string> PublicSemanticTypes = new List<string>();
            internal readonly IList<string> MappedSemanticTypes = new List<string>(); 
            internal readonly IDictionary<string, IList<string>> SemanticProperties = new Dictionary<string, IList<string>>();
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

        private static SemanticInfo RegisterModelType(Type modelType)
        {
            using (new Tracer(modelType))
            {
                _modelTypeToPropertySemanticsMapping[modelType] = ExtractPropertySemantics(modelType);

                // TODO: Combine SemanticInfo extraction with Property Semantics extraction.
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
                }

                if (semanticInfo.PublicSemanticTypes.Any())
                {
                    Log.Debug("Model type '{0}' has semantic type(s) '{1}'.", modelType.FullName, String.Join(" ", semanticInfo.PublicSemanticTypes));
                    foreach (KeyValuePair<string, IList<string>> kvp in semanticInfo.SemanticProperties)
                    {
                        Log.Debug("\tRegistered property '{0}' as semantic property '{1}'", kvp.Key, String.Join(" ", kvp.Value));
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

        private static void EnsureModelTypeRegistered(Type modelType)
            => GetSemanticInfo(modelType);

        private static SemanticInfo ExtractSemanticInfo(Type modelType)
        {
            SemanticInfo semanticInfo = new SemanticInfo();
            
            // Built-in semantic type mapping
            string bareTypeName = modelType.Name.Split('`')[0]; // Type name without generic type parameters (if any)
            semanticInfo.MappedSemanticTypes.Add(SemanticMapping.GetQualifiedTypeName(bareTypeName));

            // Extract semantic info from SemanticEntity attributes on the Model Type.
            foreach (SemanticEntityAttribute attribute in modelType.GetCustomAttributes<SemanticEntityAttribute>(inherit: true))
            {
                semanticInfo.MappedSemanticTypes.Add(SemanticMapping.GetQualifiedTypeName(attribute.EntityName, attribute.Vocab));

                if (!attribute.Public || String.IsNullOrEmpty(attribute.Prefix))
                    continue;

                string prefix = attribute.Prefix;
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

            // Extract semantic info from SemanticEntity attributes on the Model Type's properties
            foreach (MemberInfo memberInfo in modelType.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (SemanticPropertyAttribute attribute in memberInfo.GetCustomAttributes<SemanticPropertyAttribute>(inherit: true))
                {
                    if (String.IsNullOrEmpty(attribute.PropertyName))
                    {
                        // Skip properties without name.
                        continue;
                    }
                    string[] semanticPropertyNameParts = attribute.PropertyName.Split(':');
                    if (semanticPropertyNameParts.Length < 2)
                    {
                        // Skip property names without prefix.
                        continue;
                    }
                    string prefix = semanticPropertyNameParts[0];
                    if (!semanticInfo.PrefixMappings.ContainsKey(prefix))
                    {
                        // Skip property names with prefix which is not declared as public prefix on the type.
                        continue;
                    }

                    IList<string> semanticPropertyNames;
                    if (!semanticInfo.SemanticProperties.TryGetValue(memberInfo.Name, out semanticPropertyNames))
                    {
                        semanticPropertyNames = new List<string>();
                        semanticInfo.SemanticProperties.Add(memberInfo.Name, semanticPropertyNames);
                    }
                    semanticPropertyNames.Add(attribute.PropertyName);
                }
            }

            return semanticInfo;
        }

        /// <summary>
        /// Gets a mapping from property names to associated Semantic Properties for a given Model Type.
        /// </summary>
        /// <param name="modelType">The Model Type.</param>
        /// <returns>A a mapping from property names to associated Semantic Properties.</returns>
        public static Dictionary<string, List<SemanticProperty>> GetPropertySemantics(Type modelType)
        {
            EnsureModelTypeRegistered(modelType);

            Dictionary<string, List<SemanticProperty>> result;
            if (!_modelTypeToPropertySemanticsMapping.TryGetValue(modelType, out result))
            {
                throw new DxaException($"No Property Semantics found for Model Type '{modelType.FullName}'.");
            }
            return result;
        }

        /// <summary>
        /// Gets a mapping from property names to associated Semantic Properties for a given Model Type.
        /// </summary>
        /// <param name="modelType">The Model Type.</param>
        /// <returns>A a mapping from property names to associated Semantic Properties.</returns>
        private static Dictionary<string, List<SemanticProperty>> ExtractPropertySemantics(Type modelType)
        {
            IEnumerable<SemanticEntityAttribute> semanticEntityAttributes = modelType.GetCustomAttributes<SemanticEntityAttribute>();
            IDictionary<string, SemanticType> prefixToSemanticTypeMap = new Dictionary<string, SemanticType>();
            foreach (SemanticEntityAttribute semanticEntityAttribute in semanticEntityAttributes)
            {
                string prefix = semanticEntityAttribute.Prefix ?? string.Empty;
                // There may be multiple Semantic Entity attributes for the same prefix. The first one will be used.
                if (prefixToSemanticTypeMap.ContainsKey(prefix))
                {
                    Log.Debug($"Type '{modelType.FullName}' has multiple SemanticEntity attributes for prefix '{prefix}'. Ignoring '{semanticEntityAttribute.EntityName}'.");
                }
                else
                {
                    prefixToSemanticTypeMap.Add(prefix, new SemanticType(semanticEntityAttribute.EntityName, semanticEntityAttribute.Vocab));
                }
            }
            if (!prefixToSemanticTypeMap.ContainsKey(string.Empty))
            {
                // If there is no SemanticEntity attribute without prefix, we add an implicit one:
                prefixToSemanticTypeMap.Add(String.Empty, new SemanticType(modelType.Name, SemanticMapping.DefaultVocabulary));
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

            Dictionary<string, List<SemanticProperty>> result = new Dictionary<string, List<SemanticProperty>>();
            foreach (PropertyInfo property in modelType.GetProperties())
            {
                string propertyName = property.Name;

                bool ignoreMapping = false;
                bool useImplicitMapping = mapAllProperties;
                List<SemanticProperty> semanticProperties = new List<SemanticProperty>();
                foreach (SemanticPropertyAttribute semanticPropertyAttr in property.GetCustomAttributes<SemanticPropertyAttribute>(true))
                {
                    if (semanticPropertyAttr.IgnoreMapping)
                    {
                        ignoreMapping = true;
                        break;
                    }

                    if (semanticPropertyAttr.PropertyName == "_all")
                    {
                        if (!typeof(IDictionary<string, string>).IsAssignableFrom(property.PropertyType))
                        {
                            throw new DxaException(
                                $"Invalid semantics for property {modelType.Name}.{propertyName}. Properties with [SemanticProperty(\"_all\")] annotation must be of type Dictionary<string, string>."
                                );
                        }
                        semanticProperties.Add(new SemanticProperty(string.Empty, "_all", null));
                        continue;
                    }

                    string[] semanticPropertyNameParts = semanticPropertyAttr.PropertyName.Split(':');
                    string prefix;
                    string name;
                    if (semanticPropertyNameParts.Length > 1)
                    {
                        prefix = semanticPropertyNameParts[0];
                        name = semanticPropertyNameParts[1];
                    }
                    else
                    {
                        prefix = defaultPrefix;
                        name = semanticPropertyNameParts[0];
                        useImplicitMapping = false;
                    }
                    SemanticType semanticType;
                    if (!prefixToSemanticTypeMap.TryGetValue(prefix, out semanticType))
                    {
                        throw new DxaException($"Use of undeclared prefix '{prefix}' in property '{propertyName}' in type '{modelType.FullName}'.");
                    }
                    semanticProperties.Add(new SemanticProperty(prefix, name, semanticType));
                }

                if (useImplicitMapping)
                {
                    SemanticType semanticType;
                    if (!prefixToSemanticTypeMap.TryGetValue(defaultPrefix, out semanticType))
                    {
                        throw new DxaException($"Use of undeclared prefix '{defaultPrefix}' in property '{propertyName}' in type '{modelType.FullName}'.");
                    }
                    SemanticProperty implicitSemanticProperty = new SemanticProperty(
                        String.Empty, GetDefaultSemanticPropertyName(property),
                        semanticType
                        );
                    semanticProperties.Add(implicitSemanticProperty);
                }

                if (ignoreMapping || semanticProperties.Count == 0)
                {
                    continue;
                }

                if (result.ContainsKey(propertyName))
                {
                    // Properties with same name can exist is a property is reintroduced with a different signature in a subclass.
                    Log.Debug("Property with name '{0}' is declared multiple times in type {1}.", propertyName, modelType.FullName);
                    continue;
                }

                result.Add(propertyName, semanticProperties);
            }

            return result;
        }

        private static string GetDefaultSemanticPropertyName(PropertyInfo property)
        {
            // Transform Pascal case into camel case.
            string semanticPropertyName = property.Name.Substring(0, 1).ToLower() + property.Name.Substring(1);
            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && semanticPropertyName.EndsWith("s"))
            {
                // Remove trailing 's' of List property name
                semanticPropertyName = semanticPropertyName.Substring(0, semanticPropertyName.Length - 1);
            }
            return semanticPropertyName;
        }
    }
}
