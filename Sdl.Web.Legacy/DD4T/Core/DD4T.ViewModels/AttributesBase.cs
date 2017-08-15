using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.Core.Contracts.ViewModels;
using System.Reflection;
using System.Web;
using DD4T.ViewModels.Reflection;
using DD4T.ViewModels.Exceptions;
using System.Collections;
using DD4T.ViewModels;
using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels.Binding;

namespace DD4T.ViewModels.Attributes
{
    /// <summary>
    /// A Base class for an Attribute identifying a Property that represents some part of a View Model
    /// </summary>
    public abstract class ModelPropertyAttributeBase : Attribute, IPropertyAttribute
    {
        public abstract IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory);
        /// <summary>
        /// When overriden in a derived class, this property returns the expected return type of the View Model property.
        /// </summary>
        /// <remarks>Primarily used for debugging purposes. This property is used to throw an accurate exception at run time if
        /// the property return type does not match with the expected type.</remarks>
        public abstract Type ExpectedReturnType { get; }


        public IModelMapping ComplexTypeMapping
        {
            get;
            set;
        }
      }

    /// <summary>
    /// A Base class for an Attribute identifying a Property that represents a Field
    /// </summary>
    /// <remarks>
    /// The field can be content, metadata, or the metadata of a template
    /// </remarks>
    public abstract class FieldAttributeBase : ModelPropertyAttributeBase, IFieldAttribute
    {
        protected bool allowMultipleValues = false;
        protected bool inlineEditable = false;
        protected bool mandatory = false; //probably don't need this one
        protected bool isMetadata = false;
        /// <summary>
        /// Creates a new Field Attribute
        /// </summary>
        public FieldAttributeBase()
        { }
        
        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData != null)
            {
                //need null checks on Template
                IFieldSet fields = null;
                ITemplate template = null;
                if (IsTemplateMetadata)
                {
                    if (modelData is IComponentPresentation)
                    {
                        var templateData = modelData as IComponentPresentation;
                        template = templateData.ComponentTemplate;
                    }
                    else if (modelData is IPage)
                    {
                        var templateData = modelData as IPage;
                        template = templateData.PageTemplate;
                    }
                    fields = template != null ? template.MetadataFields : null;
                }
                else if (IsMetadata)
                {
                    if (modelData is IComponentPresentation)
                    {
                        fields = (modelData as IComponentPresentation).Component.MetadataFields;
                    }
                    else if (modelData is IComponent)
                    {
                        fields = (modelData as IComponent).MetadataFields;
                    }
                    else if (modelData is IPage)
                    {
                        fields = (modelData as IPage).MetadataFields;
                    }
                    else if (modelData is ITemplate)
                    {
                        fields = (modelData as ITemplate).MetadataFields;
                    }
                    else if (modelData is IKeyword)
                    {
                        fields = (modelData as IKeyword).MetadataFields;
                    }
                    //Any other things with MetadataFields?
                    
                }
                else if (modelData is IComponentPresentation)
                {
                    fields = (modelData as IComponentPresentation).Component.Fields;
                }
                else if (modelData is IComponent)
                {
                    fields = (modelData as IComponent).Fields;
                }
                else if (modelData is IEmbeddedFields)
                {
                    fields = (modelData as IEmbeddedFields).Fields;
                }
                if (String.IsNullOrEmpty(FieldName)) FieldName = GetFieldName(property.Name); //Convention over configuration by default -- Field name = Property name
                
                if (fields != null && fields.ContainsKey(FieldName))
                {   
                    result = this.GetFieldValues(fields[FieldName], property, template, factory);
                }
            }
            return result;
        }
        private string GetFieldName(string propertyName)
        {
            return propertyName.Substring(0, 1).ToLowerInvariant() + propertyName.Substring(1); //lowercase the first letter
        }
        /// <summary>
        /// When overriden in a derived class, this method should return the value of the View Model property from a Field object
        /// </summary>
        /// <param name="field">The Field</param>
        /// <param name="propertyType">The concrete type of the view model property for this attribute</param>
        /// <param name="template">The Component Template to use</param>
        /// <param name="factory">The View Model Builder</param>
        /// <returns></returns>
        public abstract IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template,IViewModelFactory factory = null);

        /// <summary>
        /// The Tridion schema field name for this property. If not used, the property name with camel casing is used
        /// e.g. public string SubTitle { get; set; } will use schema field name "subTitle" if FieldName is not specified.
        /// </summary>
        public string FieldName
        {
            get;
            set;
        }
        /// <summary>
        /// Is a metadata field. False (default) indicates this is a content field.
        /// </summary>
        public bool IsMetadata
        {
            get { return isMetadata; }
            set { isMetadata = value; }
        }
        /// <summary>
        /// Is a template metadata field (if a template exists in the context of the property).
        /// </summary>
        public bool IsTemplateMetadata { get; set; }

    }
    /// <summary>
    /// A Base class for an Attribute identifying a Property that represents some part of a Component 
    /// </summary>
    public abstract class ComponentAttributeBase : ModelPropertyAttributeBase, IComponentAttribute
    {
        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData != null)
            {
                if (modelData is IComponentPresentation)
                {
                    var cpData = modelData as IComponentPresentation;
                    if (cpData != null)
                    {
                        result = GetPropertyValues(cpData.Component, property,
                            cpData.ComponentTemplate, factory);
                    }
                }
                else if (modelData is IComponent) //Not all components come with Templates (Multimedia?)
                {
                    result = GetPropertyValues(modelData as IComponent, property, null, factory);
                }
            }
            return result;
        }
        /// <summary>
        /// When overriden in a derived class, this gets the value of the Property for a given Component
        /// </summary>
        /// <param name="component">Component for the View Model</param>
        /// <param name="propertyType">Actual return type for the Property</param>
        /// <param name="template">Component Template</param>
        /// <param name="factory">View Model factory</param>
        /// <returns>The Property value</returns>
        public abstract IEnumerable GetPropertyValues(IComponent component, IModelProperty property, ITemplate template, IViewModelFactory factory);
    }

    /// <summary>
    ///  A Base class for an Attribute identifying a Property that represents some part of a Template
    /// </summary>
    public abstract class TemplateAttributeBase : ModelPropertyAttributeBase, ITemplateAttribute
    {
        /// <summary>
        /// When overriden in a derived class, this gets the value of the Property for a given Template
        /// </summary>
        /// <param name="template">Template for the View Model</param>
        /// <param name="propertyType">Actual return type for the Property</param>
        /// <param name="factory">View Model factory</param>
        /// <returns>The Property value</returns>
        public abstract IEnumerable GetPropertyValues(ITemplate template, Type propertyType, IViewModelFactory factory);

        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData != null && modelData is IComponentPresentation
                && (modelData as IComponentPresentation).ComponentTemplate != null)
            {
                ITemplate templateData = (modelData as IComponentPresentation).ComponentTemplate;
                result = this.GetPropertyValues(templateData, property.ModelType, factory);
            }
            return result;
        }
    }

    /// <summary>
    /// A Base class for an Attribute identifying a Property that represents some part of a Page
    /// </summary>
    public abstract class PageAttributeBase : ModelPropertyAttributeBase, IPageAttribute
    {
        /// <summary>
        /// When overriden in a derived class, this gets the value of the Property for a given Page
        /// </summary>
        /// <param name="page">Page for the View Model</param>
        /// <param name="propertyType">Actual return type for the Property</param>
        /// <param name="factory">View Model factory</param>
        /// <returns>The Property value</returns>
        public abstract IEnumerable GetPropertyValues(IPage page, Type propertyType, IViewModelFactory factory);

        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData is IPage)
            {
                var pageModel = modelData as IPage;
                result = this.GetPropertyValues(pageModel, property.ModelType, factory);
            }
            return result;
        }
    }

    /// <summary>
    /// A Base class for an Attribute identifying a Property that represents some part of a Keyword
    /// </summary>
    public abstract class KeywordAttributeBase : ModelPropertyAttributeBase, IKeywordAttribute
    {
        /// <summary>
        /// When overriden in a derived class, this gets the value of the Property for a given Page
        /// </summary>
        /// <param name="page">Page for the View Model</param>
        /// <param name="propertyType">Actual return type for the Property</param>
        /// <param name="factory">View Model factory</param>
        /// <returns>The Property value</returns>
        public abstract IEnumerable GetPropertyValues(IKeyword keyword, Type propertyType, IViewModelFactory factory);

        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData is IKeyword)
            {
                var keywordModel = modelData as IKeyword;
                result = this.GetPropertyValues(keywordModel, property.ModelType, factory);
            }
            return result;
        }
    }

    /// <summary>
    /// A Base class for an Attribute identifying a Property that represents a set of Component Presentations
    /// </summary>
    /// <remarks>The View Model must be a Page</remarks>
    public abstract class ComponentPresentationsAttributeBase : ModelPropertyAttributeBase //For use in a PageModel
    {
        //Really leaving the bulk of the work to implementer -- they must both find out if the CP matches this attribute and then construct an object with it
        /// <summary>
        /// When overriden in a derived class, this gets a set of values representing Component Presentations of a Page
        /// </summary>
        /// <param name="cps">Component Presentations for the Page Model</param>
        /// <param name="propertyType">Actual return type of the Property</param>
        /// <param name="factory">A View Model factory</param>
        /// <returns>The Property value</returns>
        public abstract IEnumerable GetPresentationValues(IList<IComponentPresentation> cps, IModelProperty property, IViewModelFactory factory, IContextModel contextModel);

        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData is IPage)
            {
                var cpModels = (modelData as IPage).ComponentPresentations;
                var contextModel = factory.ContextResolver.ResolveContextModel(modelData);
                result = GetPresentationValues(cpModels, property, factory, contextModel);
            }
            return result;
        }
    }


    /// <summary>
    /// An Attribute for identifying a Content View Model. Can be based on either Embedded Schema or Component Schema
    /// </summary>
    public class ContentModelAttribute : Attribute, IContentModelAttribute 
    {
        //TODO: De-couple this from the Schema name specifically? What would make sense?
        //TOOD: Possibly change this to use purely ViewModelKey and make that an object, leave it to the key provider to assign objects with logical equals overrides

        private string schemaRootElementName;
        private bool inlineEditable = false;
        private bool isDefault = false;
        private string[] viewModelKeys;
        /// <summary>
        /// View Model
        /// </summary>
        /// <param name="schemaRootElementName">Tridion schema name for component type for this View Model</param>
        /// <param name="isDefault">Is this the default View Model for this schema. If true, Components
        /// with this schema will use this class if no other View Models' Keys match.</param>
        public ContentModelAttribute(string schemaRootElementName, bool isDefault)
        {
            this.schemaRootElementName = schemaRootElementName;
            this.isDefault = isDefault;
        }

        //Using Schema Name ties each View Model to a single Tridion Schema -- this is probably ok in 99% of cases
        //Using schema name doesn't allow us to de-couple the Model itself from Tridion however (neither does requiring
        //inheritance of IViewModel!)
        //Possible failure: if the same model was meant to represent similar parts of multiple schemas (should however
        //be covered by decent Schema design i.e. use of Embedded Schemas and Linked Components. Same fields shouldn't
        //occur repeatedly)

        public string SchemaRootElementName
        {
            get
            {
                return schemaRootElementName;
            }
        }

        /// <summary>
        /// Identifiers for further specifying which View Model to use for different presentations.
        /// </summary>
        public string[] ViewModelKeys
        {
            get { return viewModelKeys; }
            set { viewModelKeys = value; }
        }
        /// <summary>
        /// Is inline editable. Only for semantic use.
        /// </summary>
        public bool InlineEditable
        {
            get
            {
                return inlineEditable;
            }
            set
            {
                inlineEditable = value;
            }
        }

        /// <summary>
        /// Is the default View Model for the schema. If set to true, this will be the View Model to use for a given schema if no View Model ID is specified.
        /// </summary>
        public bool IsDefault { get { return isDefault; } }

        public override int GetHashCode()
        {
            return (ViewModelKeys != null ? ViewModelKeys.GetHashCode() : 0) * 37 + SchemaRootElementName.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj != null && obj is ContentModelAttribute)
            {
                ContentModelAttribute key = (ContentModelAttribute)obj;
                if (this.ViewModelKeys != null && key.ViewModelKeys != null)
                {
                    //if both have a ViewModelKey set, use both ViewModelKey and schema
                    //Check for a match anywhere in both lists
                    var match = from i in this.ViewModelKeys.Select(a => a.ToLower())
                                join j in key.ViewModelKeys.Select(a => a.ToLower())
                                on i equals j
                                select i;
                    //Schema names match and there is a matching view model ID
                    if (this.SchemaRootElementName.Equals(key.SchemaRootElementName, StringComparison.OrdinalIgnoreCase)
                        && match.Count() > 0)
                        return true;
                }
                //Note: if the parent of a linked component is using a View Model Key, the View Model
                //for that linked component must either be Default with no View Model Keys, or it must
                //have the View Model Key of the parent View Model
                if (((this.ViewModelKeys == null || this.ViewModelKeys.Length == 0) && key.IsDefault) //this set of IDs is empty and the input is default
                    || ((key.ViewModelKeys == null || key.ViewModelKeys.Length == 0) && this.IsDefault)) //input set of IDs is empty and this is default
                //if (key.IsDefault || this.IsDefault) //Fall back to default if the view model key isn't found -- useful for linked components
                {
                    //Just compare the schema names
                    return this.SchemaRootElementName.Equals(key.SchemaRootElementName, StringComparison.OrdinalIgnoreCase);
                }
            }
            return false;
        }

        public bool IsMatch(IModel data, string key)
        {
            bool result = false;
            string schemaRootElementName = null;
            if (data != null)
            {
                //Ideally we'd have a common interface for these 2 that have a Schema property
                if (data is IComponentPresentation)
                {
                    var definedData = data as IComponentPresentation;
                    schemaRootElementName = definedData.Component.Multimedia == null ? definedData.Component.Schema.RootElementName : definedData.Component.Schema.Title;
                }
                else if (data is IComponent)
                {
                    var definedData = data as IComponent;
                    schemaRootElementName = definedData.Multimedia == null ? definedData.Schema.RootElementName : definedData.Schema.Title;
                }
                else if (data is IEmbeddedFields)
                {
                    var definedData = data as IEmbeddedFields;
                    schemaRootElementName = definedData.EmbeddedSchema.RootElementName;
                }
                if (!String.IsNullOrEmpty(schemaRootElementName))
                {
                    var compare = new ContentModelAttribute(schemaRootElementName, false)
                    {
                        ViewModelKeys = key == null ? null : new string[] { key }
                    };
                    result = this.Equals(compare);
                }
            }
            return result;
        }
    }

    /// <summary>
    /// An Attribute for identifying a Page View Model
    /// </summary>
    public class PageViewModelAttribute : Attribute, IPageModelAttribute
    {
        public PageViewModelAttribute()
        {
        }
        public string[] ViewModelKeys
        {
            get;
            set;
        }
        public string TemplateTitle
        {
            get;
            set;
        }

        public bool IsMatch(IModel data, string key)
        {
            bool result = false;
            if (data is IPage)
            {
                var contentData = data as IPage;
                // if there are no view model keys defined on this model AND the page template does not specifically require a view model key, we will try to match on the page template title
                if ((ViewModelKeys == null || ViewModelKeys.Count() == 0) && string.IsNullOrEmpty(key))
                {
                    if (! string.IsNullOrEmpty(TemplateTitle))
                    {
                        return contentData.PageTemplate.Title.ToLower() == TemplateTitle.ToLower();
                    }
                }
                result = ViewModelKeys.Any(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
            }
            return result;
        }
    }

    /// <summary>
    /// An Attribute for identifying a Keyword View Model
    /// </summary>
    public class KeywordViewModelAttribute : Attribute, IKeywordModelAttribute
    {
        public KeywordViewModelAttribute(string[] viewModelKeys)
        {
            ViewModelKeys = viewModelKeys;
        }
        /// <summary>
        /// View Model Keys for this Keyword
        /// </summary>
        /// <remarks>Common View Model Keys for Keywords are Metadata Schema Title or Category Title.</remarks>
        public string[] ViewModelKeys
        {
            get;
            set;
        }

        public bool IsMatch(IModel data, string key)
        {
            bool result = false;
            if (data is IKeyword)
            {
                result = ViewModelKeys.Any(x => x.Equals(key, StringComparison.OrdinalIgnoreCase));
            }
            return result;
        }
    }

    public abstract class NestedModelFieldAttributeBase : FieldAttributeBase, ILinkablePropertyAttribute
    {
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template,IViewModelFactory factory = null)
        {
            IEnumerable fieldValue = null;
            var values = GetRawValues(field);
            if (values != null)
            {
                if (ComplexTypeMapping == null && ReturnRawData)
                    fieldValue = values;
                else
                    fieldValue = values.Cast<object>()
                        .Select(value => BuildModel(factory, BuildModelData(value, field, template), property))
                    .Where(value => value != null);
            }
            return fieldValue;
        }

        private IContextModel _contextModel;
        public IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory builder, IContextModel contextModel)
        {
            _contextModel = contextModel;
            return base.GetPropertyValues(modelData, property, builder);
        }

        protected virtual object BuildModel(IViewModelFactory factory, IModel data, IModelProperty property)
        {
            object result = null;
            if (ComplexTypeMapping != null)
            {
                result = factory.BuildMappedModel(data, ComplexTypeMapping);
            }
            else
            {
                var modelType = GetModelType(data, factory, property);
                result = modelType != null ? factory.BuildViewModel(modelType, data, _contextModel) : null;
            }
            return result;
        }

        public abstract IEnumerable GetRawValues(IField field);

        protected abstract IModel BuildModelData(object value, IField field, ITemplate template);
        protected abstract Type GetModelType(IModel data, IViewModelFactory factory, IModelProperty property);
        protected abstract bool ReturnRawData { get; }

        public override Type ExpectedReturnType
        {
            get
            {
                if (ComplexTypeMapping != null)
                    return typeof(object);
                else return typeof(IViewModel);
            }
        }

        
    }
}
