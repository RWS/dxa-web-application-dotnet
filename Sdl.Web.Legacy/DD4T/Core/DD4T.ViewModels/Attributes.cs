using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.ViewModels.Attributes;
using DD4T.Core.Contracts.ViewModels;
using DD4T.ViewModels.Reflection;
using DD4T.ViewModels;
using DD4T.ViewModels.Exceptions;
using DD4T.ContentModel;
using System.Reflection;
using System.Collections;

namespace DD4T.ViewModels.Attributes
{

    /// <summary>
    /// A Keyword component field (used for raw key data)
    /// </summary>
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
    public class ResolvedUrlFieldAttribute : FieldAttributeBase, ILinkablePropertyAttribute
    {
        //public ResolvedUrlFieldAttribute(string fieldName) : base(fieldName) { }
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            string pageId = string.Empty;
            if(_contextModel != null)
                pageId = _contextModel.PageId.ToString();

            return field.LinkedComponentValues
                .Select(x => {
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
    /// The TcmUri of the keyword 
    /// </summary>
    public class KeywordIdAttribute : KeywordAttributeBase
    {
        public override IEnumerable GetPropertyValues(IKeyword keyword, Type propertyType, IViewModelFactory factory)
        {
            return keyword == null ? null : new TcmUri[] { new TcmUri(keyword.Id) };
        }
        public override Type ExpectedReturnType
        {
            get { return typeof(TcmUri); }
        }
    }
}
