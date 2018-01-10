using DD4T.ContentModel;
using DD4T.Core.Contracts.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ViewModels.Exceptions
{
    public class ViewModelTypeNotFoundException : Exception
    {
        //public ViewModelTypeNotFoundException(IComponentPresentation data)
        //    : base(String.Format("Could not find view model for schema '{0}' and Template '{1}' or default for schema '{0}' in loaded assemblies."
        //            , data.Component.Schema.Title, data.ComponentTemplate.Title))
        //{ }
        // public ViewModelTypeNotFoundException(IPage data)
        //    : base(String.Format("Could not find view model for schema '{0}' and Template '{1}' or default for schema '{0}' in loaded assemblies."
        //            , data.Component.Schema.Title, data.ComponentTemplate.Title))
        //{ }

        //public ViewModelTypeNotFoundException(ITemplate data)
        //    : base(String.Format("Could not find view model for item with Template '{0}' and ID '{1}'", data.Title, data.Id))
        //{ }

        //TODO: REfactor to check type and use other overloads
        public ViewModelTypeNotFoundException(IModel data)
            : base("Could not find view model for {0}")
        {
            if (data is IComponentPresentation)
            {
                //Identifier = $"ComponentPresentation component.title={((IComponentPresentation)data).Component.Title}, component.id={((IComponentPresentation)data).Component.Id}, component template title={((IComponentPresentation)data).ComponentTemplate.Title}";
                var cp = (IComponentPresentation)data;
                if (cp.ComponentTemplate == null)
                    Identifier = string.Format("schema '{0} - {1}' or default for schema '{0}' in loaded assemblies. Component Template data not available, this should be a embedded or linked component type", cp.Component.Schema.Title, cp.Component.Schema.Id);
                else
                    Identifier = string.Format("schema '{0} - {1}' and Template '{2} = {3}' or default for schema '{0}' in loaded assemblies.", cp.Component.Schema.Title, cp.Component.Schema.Id, cp.ComponentTemplate.Title, cp.ComponentTemplate.Id);
            }
            else if (data is IPage)
            {
                //Identifier = $"Page title={((IPage)data).Title}, id={((IPage)data).Id}, page template title={((IPage)data).PageTemplate.Title}";
                var page = (IPage)data;
                Identifier = String.Format("Page '{0} - {1}'  in loaded assemblies.", page.PageTemplate.Title, page.PageTemplate.Id);
            }
            else if (data is ITemplate)
            {
                //Identifier = $"template title={((ITemplate)data).Title}, id={((ITemplate)data).Id}";
                var template = (ITemplate)data;
                Identifier = string.Format("item with Template '{0}' and ID '{1}'", template.Title, template.Id);
            }
        }

        public override string Message
        {
            get
            {
                return string.Format(base.Message, Identifier);
            }
        }

        public string Identifier
        {
            get; set;
        }
    }

    public class PropertyTypeMismatchException : Exception
    {
        public PropertyTypeMismatchException(IModelProperty fieldProperty, IPropertyAttribute fieldAttribute, object fieldValue) :
            base(String.Format("Type mismatch for property '{0}'. Expected type for '{1}' is {2}. Model Property is of type {3}. Field value is of type {4}."
            , fieldProperty.Name, fieldAttribute.GetType().Name, fieldAttribute.ExpectedReturnType.FullName, fieldProperty.PropertyType.FullName,
            fieldValue == null ? "" : fieldValue.GetType().FullName))
        { }
    }

    public class InvalidViewModelTypeException : Exception
    {
        public InvalidViewModelTypeException(Type type) :
            base(String.Format("Unable to initiate a type based on an interface. {0}", type.FullName))
        { }
    }
}