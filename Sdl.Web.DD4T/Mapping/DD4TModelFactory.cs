using DD4T.ContentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sdl.Web.Mvc.Models;
using Sdl.Web.Mvc.Mapping;
using Sdl.Web.DD4T.Extensions;

namespace Sdl.Web.DD4T
{
    public class DD4TModelFactory : BaseModelFactory
    {
        /// <summary>
        /// Default ModelFactory, retrieves model type and sets properties
        /// according to some generic rules
        /// </summary>
        /// <param name="view">The name of the view</param>
        /// <param name="data">The component to load data from</param>
        /// <returns></returns>
        public override object CreateEntityModel(object data, string view)
        {
            var cp = data as IComponentPresentation;
            if (cp != null)
            {
                var entityType = cp.Component.Schema.RootElementName;
                var model = GetEntity(entityType);
                var type = model.GetType();
                foreach (var field in cp.Component.Fields)
                {
                    model.SetProperty(field.Value);
                }
                foreach (var field in cp.Component.MetadataFields)
                {
                    model.SetProperty(field.Value);
                }
                return model;
            }
            else
            {
                throw new Exception(String.Format("Cannot create model for class {0}. Expecting IComponentPresentation.", data.GetType().FullName));
            }
        }

        public override object CreatePageModel(object data, string view)
        {
            IPage page = data as IPage;
            if (page != null)
            {
                WebPage model = new WebPage{Id=page.Id,Title=page.Title};
                //TODO we need some logic to set the regions, header and footer
                Region mainRegion = new Region{Name="Main"};
                mainRegion.Items.AddRange(page.ComponentPresentations);
                model.Regions.Add("Main", mainRegion);
                return model;
            }
            throw new Exception(String.Format("Cannot create model for class {0}. Expecting IPage.", data.GetType().FullName));
        }
     }
}
