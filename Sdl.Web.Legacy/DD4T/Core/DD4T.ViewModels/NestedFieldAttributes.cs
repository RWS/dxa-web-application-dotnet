using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels;
using DD4T.ViewModels.Attributes;
using DD4T.ViewModels.Exceptions;
using DD4T.ViewModels.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DD4T.ViewModels.Attributes
{
    /// <summary>
    /// An embedded schema field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class EmbeddedSchemaFieldAttribute : NestedModelFieldAttributeBase
    {
        public override IEnumerable GetRawValues(IField field)
        {
            return field.EmbeddedValues;
        }

        public Type EmbeddedModelType
        {
            get;
            set;
        }

        protected override IModel BuildModelData(object value, IField field, ITemplate template)
        {
            return new EmbeddedFields
            {
                Fields = (IFieldSet)value,
                MetadataFields = null,
                EmbeddedSchema = field.EmbeddedSchema,
                Template = template
            };
        }

        protected override Type GetModelType(IModel data, IViewModelFactory factory, IModelProperty property)
        {
            if (EmbeddedModelType == null)
            {
                return property.ModelType;
            }

            return EmbeddedModelType;
        }

        protected override bool ReturnRawData
        {
            get { return false; }
        }
    }

    /// <summary>
    /// A Component Link Field
    /// </summary>
    /// <example>
    /// To create a multi value linked component with a custom return Type:
    ///     [LinkedComponentField(FieldName = "content", LinkedComponentTypes = new Type[] { typeof(GeneralContentViewModel) }, AllowMultipleValues = true)]
    ///     public ViewModelList&lt;GeneralContentViewModel&gt; Content { get; set; }
    ///
    /// To create a single linked component using the default DD4T type:
    ///     [LinkedComponentField(FieldName = "internalLink")]
    ///     public IComponent InternalLink { get; set; }
    /// </example>
    /// <remarks>
    /// This requires the Property to be a concrete type with a constructor that implements ICollection&lt;T&gt; or is T[]
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class LinkedComponentFieldAttribute : NestedModelFieldAttributeBase
    {
        public override IEnumerable GetRawValues(IField field)
        {
            return field.LinkedComponentValues;
        }

        // TODO: remove once reading type from model property works
        public Type[] LinkedComponentTypes //Is there anyway to enforce the types passed to this?
        {
            get;
            set;
        }

        protected override IModel BuildModelData(object value, IField field, ITemplate template)
        {
            //Assuming the use of DD4T Content Model here
            var component = (Component)value;
            return new ComponentPresentation
            {
                Component = component,
                ComponentTemplate = template as ComponentTemplate
            };
        }

        protected override Type GetModelType(IModel data, IViewModelFactory factory, IModelProperty property)
        {
            if (LinkedComponentTypes == null || LinkedComponentTypes.Count() == 0)
            {
                if (!property.ModelType.IsInterface && !property.ModelType.IsAbstract)
                    return property.ModelType;

                //setting it to null in case the list initiated and contains 0 items.
                LinkedComponentTypes = null;
            }
            Type result = null;

            //throw any exception if occured. there no point to handling it over here
            result = factory.FindViewModelByAttribute<IContentModelAttribute>(data, LinkedComponentTypes);

            return result;
        }

        protected override bool ReturnRawData
        {
            get
            {
                return false;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordFieldAttribute : NestedModelFieldAttributeBase
    {
        public override IEnumerable GetRawValues(IField field)
        {
            return field.Keywords;
        }

        // TODO: remove once reading type from model property works
        public Type KeywordType { get; set; }

        protected override IModel BuildModelData(object value, IField field, ITemplate template)
        {
            var keyword = (IKeyword)value;
            return keyword;
        }

        protected override Type GetModelType(IModel data, IViewModelFactory factory, IModelProperty property)
        {
            if (KeywordType == null)
            {
                return property.ModelType;
            }
            return KeywordType;
        }

        protected override bool ReturnRawData
        {
            get
            {
                return false;
            }
        }
    }
}