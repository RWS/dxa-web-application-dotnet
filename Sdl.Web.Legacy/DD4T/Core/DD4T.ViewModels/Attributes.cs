using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels;
using DD4T.ViewModels.Attributes;
using DD4T.ViewModels.Exceptions;
using DD4T.ViewModels.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DD4T.ViewModels.Attributes
{
    /// <summary>
    /// A Keyword component field (used for raw key data)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class RawKeywordFieldAttribute : FieldAttributeBase
    {
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return field.Keywords;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(IList<IKeyword>); }
        }
    }

    /// <summary>
    /// A Multimedia component field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class MultimediaFieldAttribute : FieldAttributeBase
    {
        //public MultimediaFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return field.LinkedComponentValues.Select(x => x.Multimedia);
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(IMultimedia); }
        }
    }

    /// <summary>
    /// A text field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class TextFieldAttribute : FieldAttributeBase, ICanBeBoolean
    {
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            IEnumerable fieldValue = null;
            var values = field.Values;
            if (IsBooleanValue)
                fieldValue = values.Select(v => { bool b; return bool.TryParse(v, out b) && b; });
            else fieldValue = values;

            return fieldValue;
        }

        /// <summary>
        /// Set to true to parse the text into a boolean value.
        /// </summary>
        public bool IsBooleanValue { get; set; }

        public override Type ExpectedReturnType
        {
            get
            {
                return IsBooleanValue ? typeof(bool) : typeof(string);
            }
        }
    }

    /// <summary>
    /// A Number field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class NumberFieldAttribute : FieldAttributeBase
    {
        //public NumberFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return field.NumericValues;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(double); }
        }
    }

    /// <summary>
    /// A Date/Time field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class DateFieldAttribute : FieldAttributeBase
    {
        //public DateFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return field.DateTimeValues;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(DateTime); }
        }
    }

    /// <summary>
    /// The Key of a Keyword field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordKeyFieldAttribute : FieldAttributeBase, ICanBeBoolean
    {
        /// <summary>
        /// The Key of a Keyword field.
        /// </summary>
        /// <param name="fieldName">Tridion schema field name</param>
        //public KeywordKeyFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            IEnumerable value = null;
            var values = field.Keywords;
            if (IsBooleanValue)
                value = values.Select(k => { bool b; return bool.TryParse(k.Key, out b) && b; });
            else value = values.Select(k => k.Key);
            return value;
        }

        /// <summary>
        /// Set to true to parse the Keyword Key into a boolean value.
        /// </summary>
        public bool IsBooleanValue { get; set; }

        public override Type ExpectedReturnType
        {
            get
            {
                return IsBooleanValue ? typeof(bool) : typeof(string);
            }
        }
    }

    /// <summary>
    /// The Title of a Keyword field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordTitleFieldAttribute : FieldAttributeBase, ICanBeBoolean
    {
        /// <summary>
        /// The Key of a Keyword field.
        /// </summary>
        /// <param name="fieldName">Tridion schema field name</param>
        //public KeywordKeyFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            IEnumerable value = null;
            var values = field.Keywords;
            if (IsBooleanValue)
                value = values.Select(k => { bool b; return bool.TryParse(k.Title, out b) && b; });
            else value = values.Select(k => k.Title);
            return value;
        }

        /// <summary>
        /// Set to true to parse the Keyword title into a boolean value.
        /// </summary>
        public bool IsBooleanValue { get; set; }

        public override Type ExpectedReturnType
        {
            get
            {
                return IsBooleanValue ? typeof(bool) : typeof(string);
            }
        }
    }

    /// <summary>
    /// The Key of a Keyword as a number
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class NumericKeywordKeyFieldAttribute : FieldAttributeBase
    {
        //public NumericKeywordKeyFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return field.Keywords
                .Select(k => { double i; double.TryParse(k.Key, out i); return i; });
        }

        public override Type ExpectedReturnType
        {
            get
            {
                return typeof(double);
            }
        }
    }

    /// <summary>
    /// The URL of the Multimedia data of the view model
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class MultimediaUrlAttribute : ComponentAttributeBase
    {
        public override IEnumerable GetPropertyValues(IComponent component, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (component != null && component.Multimedia != null)
            {
                result = new string[] { component.Multimedia.Url };
            }
            return result;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(String); }
        }
    }

    /// <summary>
    /// The Multimedia data of the view model
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class MultimediaAttribute : ComponentAttributeBase
    {
        public override IEnumerable GetPropertyValues(IComponent component, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            IMultimedia result = null;
            if (component != null && component.Multimedia != null)
            {
                result = component.Multimedia;
            }
            return new IMultimedia[] { result };
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(IMultimedia); }
        }
    }

    /// <summary>
    /// The title of the Component (if the view model represents a Component)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ComponentTitleAttribute : ComponentAttributeBase
    {
        public override IEnumerable GetPropertyValues(IComponent component, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return component == null ? null : new string[] { component.Title };
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(String); }
        }
    }

    /// <summary>
    /// The title of the Page (if the view model represents a Page)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class PageTitleAttribute : PageAttributeBase
    {
        public override IEnumerable GetPropertyValues(IPage page, Type propertyType, IViewModelFactory factory)
        {
            return page == null ? null : new string[] { page.Title };
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(String); }
        }
    }

    /// <summary>
    /// A DD4T IMultimedia object representing the multimedia data of the model
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class DD4TMultimediaAttribute : ComponentAttributeBase
    {
        //Example of using the BaseData object

        public override IEnumerable GetPropertyValues(IComponent component, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            IMultimedia[] result = null;
            if (component != null && component.Multimedia != null)
            {
                result = new IMultimedia[] { component.Multimedia };
            }
            return result;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(IMultimedia); }
        }
    }

    /// <summary>
    /// All Component Presentations on a Page
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ComponentPresentationsAttribute : ComponentPresentationsAttributeBase
    {
        public override IEnumerable GetPresentationValues(IList<IComponentPresentation> cps, IModelProperty property, IViewModelFactory factory, IContextModel contextModel)
        {
            return cps.Select(cp =>
                        {
                            object model = null;
                            if (ComplexTypeMapping != null)
                            {
                                model = factory.BuildMappedModel(cp, ComplexTypeMapping);
                            }
                            else model = factory.BuildViewModel((cp));
                            return model;
                        });
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(IList<IViewModel>); }
        }
    }

    /// <summary>
    /// Component Presentations filtered by the DD4T CT Metadata "view" field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class PresentationsByViewAttribute : ComponentPresentationsAttributeBase
    {
        public override IEnumerable GetPresentationValues(IList<IComponentPresentation> cps, IModelProperty property, IViewModelFactory factory, IContextModel contextModel)
        {
            return cps.Where(cp =>
                    {
                        bool result = false;
                        if (cp.ComponentTemplate != null && cp.ComponentTemplate.MetadataFields != null
                            && cp.ComponentTemplate.MetadataFields.ContainsKey("view"))
                        {
                            var view = cp.ComponentTemplate.MetadataFields["view"].Values.FirstOrDefault();
                            if (view != null && view.StartsWith(ViewPrefix))
                            {
                                result = true;
                            }
                        }
                        return result;
                    })
                    .Select(cp =>
                        {
                            object model = null;
                            if (ComplexTypeMapping != null)
                            {
                                model = factory.BuildMappedModel(cp, ComplexTypeMapping);
                            }
                            else model = factory.BuildViewModel((cp));
                            return model;
                        });
        }

        public string ViewPrefix { get; set; }

        public override Type ExpectedReturnType
        {
            get { return typeof(IList<IViewModel>); }
        }
    }

    /// <summary>
    /// Component Presentations filtered by the DD4T CT Metadata "Region" field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class PresentationsByRegionAttribute : ComponentPresentationsAttributeBase
    {
        public override IEnumerable GetPresentationValues(IList<IComponentPresentation> cps, IModelProperty property, IViewModelFactory factory, IContextModel contextModel)
        {
            return cps.Where(cp =>
            {
                bool result = false;
                if (cp.ComponentTemplate != null && cp.ComponentTemplate.MetadataFields != null
                    && cp.ComponentTemplate.MetadataFields.ContainsKey("region"))
                {
                    var region = cp.ComponentTemplate.MetadataFields["region"].Values.Cast<string>().FirstOrDefault();
                    if (!string.IsNullOrEmpty(region) && region.Contains(Region))
                    {
                        result = true;
                    }
                }
                return result;
            })
                    .Select(cp =>
                    {
                        object model = null;
                        if (ComplexTypeMapping != null)
                        {
                            model = factory.BuildMappedModel(cp, ComplexTypeMapping);
                        }
                        else model = factory.BuildViewModel(cp, contextModel);
                        return model;
                    });
        }

        public string Region { get; set; }

        public override Type ExpectedReturnType
        {
            get { return typeof(IList<IViewModel>); }
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordDataAttribute : ModelPropertyAttributeBase
    {
        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            IEnumerable result = null;
            if (modelData != null && modelData is IKeyword)
            {
                result = new IKeyword[] { modelData as IKeyword };
            }
            return result;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(IKeyword); }
        }
    }

    /// <summary>
    /// Field that is parsed into an Enum. Can be Text field or Keyword (Key is parsed to Enum)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class EnumFieldAttribute : FieldAttributeBase
    {

        public bool RemoveWhitespace { get; set; }
        
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            var result = new List<object>();
            IEnumerable fields = new List<string>();

            if (field.Value.Any())
                fields = field.Values;
            else if (field.Keywords.Any())
                fields = field.Keywords.Select(f => f.Key);

            foreach (var value in fields)
            {
                object parsed;
                if (EnumTryParse(property.ModelType, value, out parsed))
                {
                    result.Add(parsed);
                }
            }
            return result;
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(Enum); }
        }

        private bool EnumTryParse(Type enumType, object value, out object parsedEnum)
        {
            bool result = false;
            parsedEnum = null;
            if (value != null)
            {
                try
                {
                    string valueToBeParsed = RemoveWhitespace ? RemoveWhitespaceInsideString(value.ToString()) : value.ToString();
                    parsedEnum = Enum.Parse(enumType, valueToBeParsed);
                    result = true;
                }
                catch (Exception)
                {
                    result = false;
                }
            }
            return result;
        }

        private string RemoveWhitespaceInsideString(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
    }

    /// <summary>
    /// An Attribute for a Property representing the resolved URL for a linked component or linked multimedia component
    /// </summary>
    /// <remarks>Returns the resolved URL only</remarks>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ResolvedUrlFieldAttribute : FieldAttributeBase, ILinkablePropertyAttribute
    {
        //public ResolvedUrlFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            string pageId = string.Empty;
            if (_contextModel != null)
                pageId = _contextModel.PageId.ToString();

            return field.LinkedComponentValues
                .Select(x =>
                {
                    if (IncludePage && !string.IsNullOrEmpty(pageId))
                        return factory.LinkResolver.ResolveUrl(x, pageId);
                    else
                        return factory.LinkResolver.ResolveUrl(x);
                });
        }

        public IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory builder, IContextModel contextModel)
        {
            _contextModel = contextModel;
            return base.GetPropertyValues(modelData, property, builder);
        }

        /// <summary>
        /// Includes pageId to the LinkResolving mechanism of Tridion
        /// </summary>
        public bool IncludePage { get; set; }

        private IContextModel _contextModel;

        public override Type ExpectedReturnType
        {
            get { return typeof(string); }
        }
    }

    /// <summary>
    /// The TcmUri of the component (TcmUri)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class ComponentIdAttribute : ComponentAttributeBase
    {
        public override IEnumerable GetPropertyValues(IComponent component, IModelProperty property,
                                                      ITemplate template, IViewModelFactory factory)
        {
            return component == null ? null : new TcmUri[] { new TcmUri(component.Id) };
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(TcmUri); }
        }
    }

    /// <summary>
    /// The TcmUri of the page (TcmUri)
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class PageIdAttribute : PageAttributeBase
    {
        public override IEnumerable GetPropertyValues(IPage page, Type propertyType, IViewModelFactory factory)
        {
            return page == null ? null : new TcmUri[] { new TcmUri(page.Id) };
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(TcmUri); }
        }
    }

    /// <summary>
    /// An Attribute for identifying a Content View Model. Can be based on either Embedded Schema or Component Schema
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
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
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
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
                    if (!string.IsNullOrEmpty(TemplateTitle))
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
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
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

    /// <summary>
    /// The Title of a Keyword viewmodel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordTitleAttribute : KeywordAttributeBase
    {

        public override Type ExpectedReturnType
        {
            get
            {
                return typeof(string);
            }
        }

        public override IEnumerable GetPropertyValues(IKeyword keyword, Type propertyType, IViewModelFactory factory)
        {
            return new[] { keyword.Title };
        }
    }

    /// <summary>
    /// The Description of a Keyword viewmodel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordDescriptionAttribute : KeywordAttributeBase
    {
        public override IEnumerable GetPropertyValues(IKeyword keyword, Type propertyType, IViewModelFactory builder)
        {
            return new[] { keyword.Description };
        }

        public override Type ExpectedReturnType
        {
            get
            {
                return typeof(string);
            }
        }
    }

    /// <summary>
    /// The Key of a Keyword viewmodel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordKeyAttribute : KeywordAttributeBase
    {
        public override IEnumerable GetPropertyValues(IKeyword keyword, Type propertyType, IViewModelFactory builder)
        {
            return new[] { keyword.Key };
        }

        public override Type ExpectedReturnType
        {
            get
            {
                return typeof(string);
            }
        }
    }

    /// <summary>
    /// The Id of a Keyword viewmodel as string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true)]
    public class KeywordIdAttribute : KeywordAttributeBase
    {
        public override IEnumerable GetPropertyValues(IKeyword keyword, Type propertyType, IViewModelFactory builder)
        {
            IEnumerable value;
            if (propertyType == typeof(TcmUri))
                value = new[] {new TcmUri(keyword.Id)};
            else
                value = new[] {keyword.Id};

            return value;
        }

        public override Type ExpectedReturnType
        {
            get
            {
                return typeof(string);
            }
        }
    }

  }

