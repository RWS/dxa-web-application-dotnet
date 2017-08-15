//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Linq.Expressions;
//using System.Text;
//using System.Web;
//using DD4T.ViewModels.Reflection;
//using DD4T.ViewModels.Attributes;
//using DD4T.ViewModels.Contracts;
//using System.Reflection;
//using System.Collections;
//using DD4T.ViewModels.XPM;
//using DD4T.ViewModels;
//using DD4T.ContentModel;

//namespace DD4T.ViewModels.XPM
//{
//    /// <summary>
//    /// Extension methods for rendering XPM Markup in conjuction with DD4T Domain View Models
//    /// </summary>
//    public static class XpmExtensions
//    {
//        private static IXpmMarkupService xpmMarkupService = new XpmMarkupService();
//        private static IViewModelResolver resolver = ViewModelDefaults.ModelResolver;
//        /// <summary>
//        /// Gets or sets the XPM Markup Service used to render the XPM Markup for the XPM extension methods
//        /// </summary>
//        public static IXpmMarkupService XpmMarkupService
//        {
//            get { return xpmMarkupService; }
//            set { xpmMarkupService = value; }
//        }
//        #region public extension methods
//        /// <summary>
//        /// Renders both XPM Markup and Field Value 
//        /// </summary>
//        /// <typeparam name="TModel">Model type</typeparam>
//        /// <typeparam name="TProp">Property type</typeparam>
//        /// <param name="model">Model</param>
//        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
//        /// <param name="index">Optional index for a multi-value field</param>
//        /// <returns>XPM Markup and field value</returns>
//        public static HtmlString XpmEditableField<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IViewModel
//        {
//            var renderer = new XpmRenderer<TModel>(model, XpmMarkupService, resolver);
//            return renderer.XpmEditableField(propertyLambda, index);
//        }
//        /// <summary>
//        /// Renders both XPM Markup and Field Value for a multi-value field
//        /// </summary>
//        /// <typeparam name="TModel">Model type</typeparam>
//        /// <typeparam name="TProp">Property type</typeparam>
//        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
//        /// <param name="model">Model</param>
//        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
//        /// <param name="item">The particular value of the multi-value field</param>
//        /// <example>
//        /// foreach (var content in model.Content)
//        /// {
//        ///     @model.XpmEditableField(m => m.Content, content);
//        /// }
//        /// </example>
//        /// <returns>XPM Markup and field value</returns>
//        public static HtmlString XpmEditableField<TModel, TProp, TItem>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, TItem item) 
//            where TModel : IViewModel
//        {
//            var renderer = new XpmRenderer<TModel>(model, XpmMarkupService, resolver);
//            return renderer.XpmEditableField(propertyLambda, item);
//        }
//        /// <summary>
//        /// Renders the XPM markup for a field
//        /// </summary>
//        /// <typeparam name="TModel">Model type</typeparam>
//        /// <typeparam name="TProp">Property type</typeparam>
//        /// <param name="model">Model</param>
//        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
//        /// <param name="index">Optional index for a multi-value field</param>
//        /// <returns>XPM Markup</returns>
//        public static HtmlString XpmMarkupFor<TModel, TProp>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, int index = -1) where TModel : IViewModel
//        {
//            var renderer = new XpmRenderer<TModel>(model, XpmMarkupService, resolver);
//            return renderer.XpmMarkupFor(propertyLambda, index);
//        }
//        /// <summary>
//        /// Renders XPM Markup for a multi-value field
//        /// </summary>
//        /// <typeparam name="TModel">Model type</typeparam>
//        /// <typeparam name="TProp">Property type</typeparam>
//        /// <typeparam name="TItem">Item type - this must match the generic type of the property type</typeparam>
//        /// <param name="model">Model</param>
//        /// <param name="propertyLambda">Lambda expression representing the property to render. This must be a direct property of the model.</param>
//        /// <param name="item">The particular value of the multi-value field</param>
//        /// <example>
//        /// foreach (var content in model.Content)
//        /// {
//        ///     @model.XpmMarkupFor(m => m.Content, content);
//        ///     @content;
//        /// }
//        /// </example>
//        /// <returns>XPM Markup</returns>
//        public static HtmlString XpmMarkupFor<TModel, TProp, TItem>(this TModel model, Expression<Func<TModel, TProp>> propertyLambda, TItem item) 
//            where TModel : IViewModel
//        {
//            var renderer = new XpmRenderer<TModel>(model, XpmMarkupService, resolver);
//            return renderer.XpmMarkupFor(propertyLambda, item);  
//        }
//        /// <summary>
//        /// Renders the XPM Markup for a Component Presentation
//        /// </summary>
//        /// <param name="model">Model</param>
//        /// <param name="region">Region</param>
//        /// <returns>XPM Markup</returns>
//        public static HtmlString StartXpmEditingZone(this IViewModel model, string region = null)
//        {
//            HtmlString result = null;
//            if (model.ModelData is IComponentPresentation)
//            {
//                var renderer = new XpmRenderer<IViewModel>(model, XpmMarkupService, resolver);
//                result = renderer.StartXpmEditingZone(region);
//            }
//            return result;
//        }
//        #endregion
//    }


//}
