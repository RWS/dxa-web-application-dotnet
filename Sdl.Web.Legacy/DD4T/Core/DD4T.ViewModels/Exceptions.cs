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
        public ViewModelTypeNotFoundException(IComponentPresentation data)
            : base(String.Format("Could not find view model for schema '{0}' and Template '{1}' or default for schema '{0}' in loaded assemblies."
                    , data.Component.Schema.Title, data.ComponentTemplate.Title)) 
        { }

        public ViewModelTypeNotFoundException(ITemplate data)
            : base(String.Format("Could not find view model for item with Template '{0}' and ID '{1}'", data.Title, data.Id))
        { }

        //TODO: REfactor to check type and use other overloads
        public ViewModelTypeNotFoundException(IModel data)
            : base(String.Format("Could not find view model for item with Publication ID '{0}'", data))
        {

            if (data is IComponentPresentation)
            {
                Identifier = $"ComponentPresentation component.title={((IComponentPresentation)data).Component.Title}, component.id={((IComponentPresentation)data).Component.Id}, component template title={((IComponentPresentation)data).ComponentTemplate.Title}";
            }
            else if (data is IPage)
            {
                Identifier = $"Page title={((IPage)data).Title}, id={((IPage)data).Id}, page template title={((IPage)data).PageTemplate.Title}";
            }

            else if (data is ITemplate)
            {
                Identifier = $"template title={((ITemplate)data).Title}, id={((ITemplate)data).Id}";
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
            fieldValue == null ? "" : fieldValue.GetType().FullName)) { }
    }
}
