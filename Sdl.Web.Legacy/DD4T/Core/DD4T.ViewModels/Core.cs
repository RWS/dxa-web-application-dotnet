using DD4T.Core.Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using DD4T.ViewModels.Attributes;
using System.Web;
using DD4T.ViewModels.Exceptions;
using DD4T.ViewModels.Reflection;
using System.Linq.Expressions;
using DD4T.ViewModels.Binding;
using System.Collections;
using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels.Binding;
using DD4T.ContentModel.Contracts.Configuration;

namespace DD4T.ViewModels
{
    ///// <summary>
    ///// A static container class for default implementations of the View Model Framework for convenience.
    ///// </summary>
    //public static class ViewModelDefaults
    //{
    //    //Singletons
    //    private static readonly IViewModelKeyProvider keyProvider =
    //        new WebConfigViewModelKeyProvider("DD4T.ViewModels.ViewModelKeyFieldName");

    //    private static readonly IReflectionHelper reflectionHelper = new ReflectionOptimizer();
    //    private static readonly IViewModelResolver resolver = new DefaultViewModelResolver(reflectionHelper);
    //    //private static readonly IViewModelBuilder viewModelBuilder = new ViewModelBuilder(keyProvider, resolver);
    //    private static readonly IViewModelFactory factory = new ViewModelFactory(keyProvider, resolver);
    //    //private static readonly ITypeResolver typeResolver = new DefaultResolver(reflectionHelper);
    //    /// <summary>
    //    /// Default View Model Builder. 
    //    /// <remarks>
    //    /// Set View Model Key Component Template Metadata field in Web config
    //    /// with key "DD4T.DomainModels.ViewModelKeyFieldName". Defaults to field name "viewModelKey".
    //    /// </remarks>
    //    /// </summary>
    //    public static IViewModelFactory Factory { get { return factory; } }
    //    /// <summary>
    //    /// Default View Model Key Provider. 
    //    /// <remarks>
    //    /// Gets View Model Key from Component Template Metadata with field
    //    /// name specified in Web config App Settings wtih key "DD4T.DomainModels.ViewModelKeyFieldName".
    //    /// Defaults to field name "viewModelKey".
    //    /// </remarks>
    //    /// </summary>
    //    public static IViewModelKeyProvider ViewModelKeyProvider { get { return keyProvider; } }
    //    /// <summary>
    //    /// Default View Model Resolver
    //    /// </summary>
    //    /// <remarks>Resolves View Models with default parameterless constructor. If none 
    //    /// exists, it will throw an Exception.</remarks>
    //    public static IViewModelResolver ModelResolver { get { return resolver; } }
    //    /// <summary>
    //    /// Optimized Reflection Helper that caches results of resource-heavy tasks (e.g. MemberInfo.GetCustomAttributes)
    //    /// </summary>
    //    public static IReflectionHelper ReflectionCache { get { return reflectionHelper; } }
    //    /// <summary>
    //    /// Creates a new Model Mapping object
    //    /// </summary>
    //    /// <typeparam name="T">Type of model for the model mapping</typeparam>
    //    /// <returns>New Model Mapping</returns>
    //    public static IModelMapping CreateModelMapping<T>() where T : class
    //    {
    //        return new DefaultModelMapping(resolver, reflectionHelper, typeof(T));
    //    }
    //    //public static ITypeResolver TypeResolver { get { return typeResolver; } }

    //}

    /// <summary>
    /// Base View Model Key Provider implementation with no external dependencies. Set protected 
    /// string ViewModelKeyField to CT Metadata Field name to use to retrieve View Model Keys.
    /// </summary>
    public abstract class ViewModelKeyProviderBase : IViewModelKeyProvider
    {
        protected string ViewModelKeyField = string.Empty;
        public string GetViewModelKey(IModel modelData)
        {
            string result = null;
            if (modelData != null)
            {
                ITemplate template = null;
                if (modelData is IComponentPresentation)
                {
                    template = (modelData as IComponentPresentation).ComponentTemplate;
                }
                else if (modelData is IPage)
                {
                    template = (modelData as IPage).PageTemplate;
                }
                else if (modelData is ITemplate)
                {
                    template = modelData as ITemplate;
                }

                if (template != null)
                {
                    if (template.MetadataFields != null && template.MetadataFields.ContainsKey(ViewModelKeyField))
                    {
                        result = template.MetadataFields[ViewModelKeyField].Values.Cast<string>().FirstOrDefault();
                    }
                }
                else if (modelData is IKeyword)
                {
                    var keyword = modelData as IKeyword;
                    //TODO: Implement metadata schema and Category
                    //if (keyword.Metadata != null && keyword.MetadataSchema != null) //If there is a metadata schema
                    //{
                    //    result = keyword.MetadataSchema.Title;
                    //}
                    //TODO: using Category would be more elegant
                    if (keyword != null && keyword.TaxonomyId != null) //If there's no metadata schema fall back to the Taxonomy ID
                        result = keyword.TaxonomyId;
                }
            }
            return string.IsNullOrEmpty(result) ? null : result; 
        }
    }



    /// <summary>
    /// Implementation of View Model Key Provider that uses the Web Config app settings 
    /// to retrieve the name of the Component Template Metadata field for the view model key.
    /// Default CT Metadata field name is "viewModelKey"
    /// </summary>
    public class WebConfigViewModelKeyProvider : ViewModelKeyProviderBase
    {
        public WebConfigViewModelKeyProvider(IDD4TConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");

            ViewModelKeyField = configuration.ViewModelKeyField;
            if (string.IsNullOrEmpty(ViewModelKeyField)) ViewModelKeyField = "viewModelKey"; //Default value
        } 
    }

    /// <summary>
    /// Data structure for efficient use of Properties marked with a IPropertyAttribute Custom Attribute
    /// </summary>
    public class ModelProperty : IModelProperty
    {
        /// <summary>
        /// Name of the Property
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Setter delegate
        /// </summary>
        public Action<object, object> Set { get; set; }
        /// <summary>
        /// Getter delegate
        /// </summary>
        public Func<object, object> Get { get; set; }
        /// <summary>
        /// The DD4T PropertyAttribute of the Property
        /// </summary>
        public IPropertyAttribute PropertyAttribute { get; set; }
        /// <summary>
        /// The return Type of the Property
        /// </summary>
        public Type PropertyType { get; set; }
        public Type ModelType { get; set; }
        public bool IsEnumerable { get; set; }
        public bool IsCollection { get; set; }
        public Action<object, object> AddToCollection
        {
            get;
            set;
        }
        public bool IsArray
        {
            get;
            set;
        }
        public Func<IEnumerable, Array> ToArray { get; set; }
    }

    public class EmbeddedFields : IEmbeddedFields
    {
        public IFieldSet Fields { get; set; }

        public ISchema EmbeddedSchema { get; set; }

        public ITemplate Template { get; set; }

        public IFieldSet MetadataFields { get; set; }

        public IDictionary<string, IFieldSet> ExtensionData { get; set; }

        //public int PublicationNumber
        //{
        //    //Todo: Sia
        //    get { return Template == null ? -1 : 0; } //Template.PublicationNumber; }
        //}

    }
}
