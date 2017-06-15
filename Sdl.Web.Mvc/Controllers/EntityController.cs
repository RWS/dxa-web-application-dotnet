using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Web.Mvc;
using Sdl.Web.Common.Logging;
using Sdl.Web.Common.Models;
using System.Web.Helpers;
using Sdl.Web.Mvc.OutputCache;

namespace Sdl.Web.Mvc.Controllers
{
    public class EntityController : BaseController
    {
        /// <summary>
        /// Renders an Entity Model
        /// </summary>
        /// <param name="entity">The entity model</param>
        /// <param name="containerSize">The size (in grid units) of the container the entity is in</param>
        /// <returns>Rendered entity model</returns>
        [HandleSectionError(View = "SectionError")]
        [DxaOutputCache]
        public virtual ActionResult Entity(EntityModel entity, int containerSize = 0)
        {
            SetupViewData(entity, containerSize);

            ViewModel model = EnrichModel(entity);
            if (model is RedirectModel)
            {
                // Force a redirect (ASP.NET MVC doesn't allow child actions to redirect using RedirectResult)
                Response.StatusCode = (int) HttpStatusCode.Redirect;
                Response.RedirectLocation = ((RedirectModel) model).RedirectUrl;
                Response.End();
                return null;
            }

            ViewData.Model = model;

            return View(model.MvcData.ViewName, model);
        }

        /// <summary>
        /// Maps Form data (for an HTTP POST request) to properies of a given Entity Model and performs basic validation.
        /// </summary>
        /// <param name="model">The Entity Model to map the form data to.</param>
        /// <returns><c>true</c> if there is any form data to be mapped.</returns>
        protected bool MapRequestFormData(EntityModel model)
        {
            if (Request.HttpMethod != "POST")
            {
                return false;
            }

            // CSRF protection: If the anti CSRF cookie is present, a matching token must be in the form data too.
            const string antiCsrfToken = "__RequestVerificationToken";
            if (Request.Cookies[antiCsrfToken] != null)
            {
                AntiForgery.Validate();
            }

            Type modelType = model.GetType();
            foreach (string formField in Request.Form)
            {
                if (formField == antiCsrfToken)
                {
                    // This is not a form field, but the anti CSRF token (already validated above).
                    continue;
                }

                PropertyInfo modelProperty = modelType.GetProperty(formField);
                if (modelProperty == null)
                {
                    Log.Debug("Model [{0}] has no property for form field '{1}'", model, formField);
                    continue;
                }

                string formFieldValue = Request.Form[formField];

                ValidationAttribute validationAttr = modelProperty.GetCustomAttribute<ValidationAttribute>();
                if (validationAttr != null)
                {
                    try
                    {
                        validationAttr.Validate(formFieldValue, formField);
                    }
                    catch (ValidationException ex)
                    {
                        string validationMessage = ResolveValidationMessage(ex.Message, model);
                        Log.Debug("Validation of property '{0}' failed: {1}", formField, validationMessage);
                        ModelState.AddModelError(formField, validationMessage);
                        continue;
                    }
                }

                try
                {
                    if (modelProperty.PropertyType == typeof (bool))
                    {
                        // The @Html.CheckBoxFor method includes a hidden field with the original checkbox state, resulting in two boolean values (comma separated)
                        formFieldValue = formFieldValue.Split(',')[0];
                    }
                    modelProperty.SetValue(model, Convert.ChangeType(formFieldValue, modelProperty.PropertyType));
                }
                catch (Exception ex)
                {
                    Log.Debug("Failed to set Model [{0}] property '{1}' to value obtained from form data: '{2}'. {3}", model, formField, formFieldValue, ex.Message);
                    ModelState.AddModelError(formField, ex.Message);
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves CM-managed validation messages. 
        /// </summary>
        /// <param name="inputMessage">The input validation message which may have the special syntax <c>@Model.{$ModelPropertyName}</c>.</param>
        /// <param name="model">The View Model used to resolve the value of such Model property expression.</param>
        /// <returns>The resolved validation message or the input message in case resolving fails.</returns>
        private static string ResolveValidationMessage(string inputMessage, ViewModel model)
        {
            const string modelPrefix = "@Model.";
            if (!inputMessage.StartsWith(modelPrefix))
            {
                return inputMessage;
            }

            string modelPropertyName = inputMessage.Substring(modelPrefix.Length);
            PropertyInfo validationMessageProperty = model.GetType().GetProperty(modelPropertyName);
            string validationMessagePropertyValue = null;
            if (validationMessageProperty != null)
            {
                validationMessagePropertyValue = validationMessageProperty.GetValue(model) as string;
            }

            if (string.IsNullOrEmpty(validationMessagePropertyValue))
            {
                Log.Warn("No validation message could be resolved for expression '{0}'", inputMessage);
                return inputMessage;
            }

            return validationMessagePropertyValue;
        }
    }
}
