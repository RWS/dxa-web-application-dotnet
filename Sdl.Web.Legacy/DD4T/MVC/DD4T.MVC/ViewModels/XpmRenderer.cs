using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DD4T.Core.Contracts.ViewModels;
using System.Web;
using System.Linq.Expressions;
using System.Collections;
using DD4T.ViewModels.Reflection;
using System.Reflection;
using DD4T.ViewModels;
using DD4T.ContentModel;
using DD4T.Mvc.ViewModels.XPM;
using System.Web.Mvc;

namespace DD4T.Mvc.ViewModels.XPM
{
    /// <summary>
    /// Renders XPM Markup for View Models
    /// </summary>
    /// <typeparam name="TModel">Type of the View Model</typeparam>
    public class XpmRenderer<TModel> : IXpmRenderer<TModel> where TModel : IViewModel
    {
        //This is just an OO implementation of the static extension methods... which one is better
        private readonly IViewModel model;
        //private readonly IComponentPresentation contentData = null;
        private readonly IModel contentModel = null;
        private readonly IXpmMarkupService xpmMarkupService;
        private readonly IViewModelResolver resolver;
        private readonly IReflectionHelper reflectionHelper;
        public XpmRenderer(IViewModel model, IXpmMarkupService service, IViewModelResolver resolver, IReflectionHelper reflectionHelper)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (service == null) throw new ArgumentNullException("service");
            if (resolver == null) throw new ArgumentNullException("resolver");
            if (reflectionHelper == null) throw new ArgumentNullException("reflectionHelper");
            this.model = model;
            if (model.ModelData is IModel)
            {
                contentModel = model.ModelData as IModel;
            }
            //if (model.ModelData is IComponentPresentation)
            //{
            //    contentData = model.ModelData as IComponentPresentation;
            //}
            this.reflectionHelper = reflectionHelper;
            this.xpmMarkupService = service;
            this.resolver = resolver;
        }

        private IFieldSet resolveFields(IModel model, bool isMetadata)
        {
            IFieldSet fields = null;
            if (model is IComponentPresentation)
            {
                var cp = model as IComponentPresentation;
                fields = isMetadata ? cp.Component.MetadataFields : cp.Component.Fields;
            }
            else if (model is IEmbeddedFields)
            {
                var emb = model as IEmbeddedFields;
                fields = emb.Fields;
            }
            return fields;
        }

        /// <summary>
        /// Gets or sets the XPM Markup Service used to render the XPM Markup for the XPM extension methods
        /// </summary>
        public IXpmMarkupService XpmMarkupService
        {
            get { return xpmMarkupService; }
        }
        #region XPM Renderer methods
        /// <summary>
        /// Renders both XPM Markup and Field Value 
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup and field value</returns>
        public HtmlString XpmEditableField<TProp>(Expression<Func<TModel, TProp>> propertyLambda, int index = -1)
        {
            HtmlString result = null;
            var modelProp = GetModelProperty(propertyLambda);
            if (modelProp.PropertyAttribute is IFieldAttribute)
            {
                var fieldProp = modelProp.PropertyAttribute as IFieldAttribute;
                //var fields = fieldProp.IsMetadata ? contentData.Component.MetadataFields : contentData.Component.Fields;
                var fields = resolveFields(contentModel, fieldProp.IsMetadata);
                result = SiteEditable<TProp>(model, fields, modelProp, index);
            }
            return result;
        }
        /// <summary>
        /// Renders both XPM Markup and Field Value for a multi-value field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="item">The particular value of the multi-value field</param>
        /// <example>
        /// foreach (var content in model.Content)
        /// {
        ///     @model.XpmEditableFieldField(m => m.Content, content);
        /// }
        /// </example>
        /// <returns>XPM Markup and field value</returns>
        public HtmlString XpmEditableField<TProp, TItem>(Expression<Func<TModel, TProp>> propertyLambda, TItem item)
        {
            HtmlString result = null;
            var modelProp = GetModelProperty(propertyLambda);
            if (modelProp.PropertyAttribute is IFieldAttribute)
            {
                var fieldProp = modelProp.PropertyAttribute as IFieldAttribute;
                //var fields = fieldProp.IsMetadata ? contentData.Component.MetadataFields : contentData.Component.Fields;
                var fields = resolveFields(contentModel, fieldProp.IsMetadata);
                int index = IndexOf(modelProp, model, item);
                result = SiteEditable<TProp>(model, fields, modelProp, index);
            }
            return result;
        }
        /// <summary>
        /// Renders the XPM markup for a field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="index">Optional index for a multi-value field</param>
        /// <returns>XPM Markup</returns>
        public HtmlString XpmMarkupFor<TProp>(Expression<Func<TModel, TProp>> propertyLambda, int index = -1)
        {
            HtmlString result = null;
            if (IsSiteEditEnabled(model))
            {

                var modelProp = GetModelProperty(propertyLambda);
                if (modelProp.PropertyAttribute is IFieldAttribute)
                {
                    var fieldProp = modelProp.PropertyAttribute as IFieldAttribute;
                    //var fields = fieldProp.IsMetadata ? contentData.Component.MetadataFields : contentData.Component.Fields;
                    var fields = resolveFields(contentModel, fieldProp.IsMetadata);
                    result = XpmMarkupFor(fields, modelProp, index);
                }
            }
            return result;
        }
        /// <summary>
        /// Renders XPM Markup for a multi-value field
        /// </summary>
        /// <typeparam name="TModel">Model type</typeparam>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
        /// <param name="model">Model</param>
        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
        /// <param name="item">The particular value of the multi-value field</param>
        /// <example>
        /// foreach (var content in model.Content)
        /// {
        ///     @model.XpmMarkupFor(m => m.Content, content);
        ///     @content;
        /// }
        /// </example>
        /// <returns>XPM Markup</returns>
        public HtmlString XpmMarkupFor<TProp, TItem>(Expression<Func<TModel, TProp>> propertyLambda, TItem item)
        {
            HtmlString result = null;
            if (IsSiteEditEnabled(model))
            {

                var modelProp = GetModelProperty(propertyLambda);
                if (modelProp.PropertyAttribute is IFieldAttribute)
                {
                    var fieldProp = modelProp.PropertyAttribute as IFieldAttribute;
                    //var fields = fieldProp.IsMetadata ? contentData.Component.MetadataFields : contentData.Component.Fields;
                    var fields = resolveFields(contentModel, fieldProp.IsMetadata);
                    int index = IndexOf(modelProp, model, item);
                    result = XpmMarkupFor(fields, modelProp, index);
                }
            }
            return result;
        }
        /// <summary>
        /// Renders the XPM Markup for a Component Presentation
        /// </summary>
        /// <param name="model">Model</param>
        /// <param name="region">Region</param>
        /// <returns>XPM Markup</returns>
        public HtmlString StartXpmEditingZone()
        {
            if (model.ModelData is IComponentPresentation)
                return new HtmlString(XpmMarkupService.RenderXpmMarkupForComponent(((IComponentPresentation)model.ModelData)));
            else return null;
        }
        #endregion

        #region private methods
        private bool IsSiteEditEnabled(IViewModel model)
        {
            //TODO: Fix this
            return xpmMarkupService.IsSiteEditEnabled();
        }

        private int IndexOf(IEnumerable enumerable, object obj)
        {
            if (obj != null)
            {
                int i = 0;
                foreach (var item in enumerable)
                {
                    if (item.Equals(obj)) return i;
                    i++;
                }
            }
            return -1;
        }
        private int IndexOf<T>(IModelProperty fieldProp, object model, T item)
        {
            int index = -1;
            object value = fieldProp.Get(model);
            if (value is IEnumerable<T>)
            {
                IEnumerable<T> list = (IEnumerable<T>)value;
                index = IndexOf(list, item);
            }
            else throw new FormatException(String.Format("Generic type of property type {0} does not match generic type of item {1}", value.GetType().Name, typeof(T).Name));
            return index;
        }
        private IModelProperty GetModelProperty<TProp>(Expression<Func<TModel, TProp>> propertyLambda)
        {
            PropertyInfo property = reflectionHelper.GetPropertyInfo(propertyLambda);
            return GetModelProperty(model.GetType(), property);
        }
        private HtmlString SiteEditable<TProp>(IViewModel model, IFieldSet fields, IModelProperty fieldProp, int index)
        {
            string markup = string.Empty;
            object value = null;
            string propValue = string.Empty;
            try
            {
                var field = GetField(fields, fieldProp);
                markup = IsSiteEditEnabled(model) ? GenerateSiteEditTag(field, index) : string.Empty;
                value = fieldProp.Get(model);
                propValue = value == null ? string.Empty : value.ToString();
            }
            catch (NullReferenceException)
            {
                return null;
            }
            return new HtmlString(markup + propValue);
        }

        private string GenerateSiteEditTag(IField field, int index)
        {
            return XpmMarkupService.RenderXpmMarkupForField(field, index);
        }
        private IField GetField(IFieldSet fields, IModelProperty fieldProp)
        {
            IField result = null;
            if (fieldProp.PropertyAttribute is IFieldAttribute)
            {
                var fieldName = ((IFieldAttribute)fieldProp.PropertyAttribute).FieldName;
                result = fields.ContainsKey(fieldName) ? fields[fieldName] : null;
            }
            return result;
        }

        private IModelProperty GetModelProperty(Type type, PropertyInfo property)
        {
            var props = resolver.GetModelProperties(type);
            return props.FirstOrDefault(x => x.Name == property.Name);
        }

        private HtmlString XpmMarkupFor(IFieldSet fields, IModelProperty fieldProp, int index)
        {
            try
            {
                return new HtmlString(GenerateSiteEditTag(GetField(fields, fieldProp), index));
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
        #endregion
    }
}