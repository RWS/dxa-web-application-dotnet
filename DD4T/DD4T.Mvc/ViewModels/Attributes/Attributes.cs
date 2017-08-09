using System.Linq;
using System.Web.Mvc;
using DD4T.Mvc.Html;
using DD4T.ViewModels.Attributes;
using System.Collections;
using DD4T.Core.Contracts.ViewModels;
using System;
using DD4T.ViewModels;
using System.Text.RegularExpressions;
using DD4T.ContentModel.Contracts.Configuration;
using DD4T.ContentModel;

namespace DD4T.Mvc.ViewModels.Attributes
{
    /// <summary>
    /// An Attribute for a Property representing the Link Resolved URL for a Linked or Multimedia Component
    /// </summary>
    /// <remarks>Uses the default DD4T GetResolvedUrl extension method. To override behavior you must implement
    /// your own Field Attribute. Future DD4T versions will hopefully allow for IoC of this implementation.</remarks>
    [Obsolete("Attribute is moved to DD4T.Core. please use 'DD4T.ViewModels.Attributes.ResolvedUrlFieldAttribute'")]
    public class ResolvedUrlFieldAttribute : FieldAttributeBase
    {
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            return field.LinkedComponentValues
                .Select(x => x.GetResolvedUrl());
        }

        public override Type ExpectedReturnType
        {
            get { return typeof(string); }
        }

    }

    /// <summary>
    /// A Rich Text field. Uses the default ResolveRichText extension method.
    /// </summary>
    /// <remarks>This Attribute is dependent on a specific implementation for resolving Rich Text. 
    /// In future versions of DD4T, the rich text resolver will hopefully be abstracted to allow for IoC, 
    /// but for now, to change the behavior you must implement your own Attribute.</remarks>
    public class RichTextFieldAttribute : FieldAttributeBase, ILinkablePropertyAttribute
    {
        public override IEnumerable GetFieldValues(IField field, IModelProperty property, ITemplate template, IViewModelFactory factory)
        {
            string pageId = null;
            if (_contextModel != null)
                pageId = _contextModel.PageId.ToString();

            return field.Values
                .Select(v => new MvcHtmlString(factory.RichTextResolver.Resolve(v, pageId).ToString())); 
        }

        public IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory builder, IContextModel contextModel)
        {
            _contextModel = contextModel;
            return base.GetPropertyValues(modelData, property, builder);
        }

        private IContextModel _contextModel;

        public override Type ExpectedReturnType
        {
            get { return  typeof(MvcHtmlString); }
        }
    }

    public class RenderDataAttribute : ModelPropertyAttributeBase
    {
        private IDD4TConfiguration DD4TConfiguration { get; set; }

        public RenderDataAttribute()
        {
            DD4TConfiguration = DependencyResolver.Current.GetService<IDD4TConfiguration>();
        }
        private string DefaultController
        {
            get
            {
                return DD4TConfiguration.ComponentPresentationController;
            }
        }
        private string DefaultControllerAction
        {
            get
            {
                return DD4TConfiguration.ComponentPresentationAction;
            }
        }
        public override IEnumerable GetPropertyValues(IModel modelData, IModelProperty property, IViewModelFactory factory)
        {
            // escape
            if (modelData == null) return null;

            ITemplate template = null;
            if (modelData is IComponentPresentation)
            {
                template = ((IComponentPresentation)modelData).ComponentTemplate;
            }
            else if (modelData is IPage)
            {
                template = ((IPage)modelData).PageTemplate;
            }

            // Run away run away
            if (template == null) return null;


            var renderData = new RenderData();

            // TODO reuse logic from page rendering
            var view = Regex.Replace(template.Title, @"\[.*\]|\s", String.Empty);
            //Todo: make it configurable
            var action = DefaultControllerAction;
            var controller = DefaultController;
            var viewFieldName = "view";
            var actionFieldName = "action";
            var controllerFieldName = "controller";

            var fields = template.MetadataFields;
            if (fields != null)
            {
                if (fields.ContainsKey(viewFieldName))
                {
                    view = fields[viewFieldName].Value;
                }
                if (fields.ContainsKey(controllerFieldName))
                {
                    controller = fields[controllerFieldName].Value;
                }
                if (fields.ContainsKey(actionFieldName))
                {
                    action = fields[actionFieldName].Value;
                }
            }
            renderData.View = view;
            renderData.Action = action;
            renderData.Controller = controller;

            return new[] { renderData };
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