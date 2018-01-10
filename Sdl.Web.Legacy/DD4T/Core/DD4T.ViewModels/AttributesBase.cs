using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using DD4T.Core.Contracts.ViewModels.Binding;
using DD4T.ViewModels;
using DD4T.ViewModels.Exceptions;
using DD4T.ViewModels.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

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
        public abstract IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory = null);

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

    public abstract class NestedModelFieldAttributeBase : FieldAttributeBase, ILinkablePropertyAttribute
    {
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory = null)
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