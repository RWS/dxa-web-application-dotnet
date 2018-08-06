using System;
using System.Collections.Generic;
using Sdl.Web.Common.Interfaces;
using Sdl.Web.DataModel;

namespace Sdl.Web.Tridion.ModelService
{
    internal class Binder : DataModelBinder
    {
        private readonly List<IDataModelExtension> _dataModelExtensions = new List<IDataModelExtension>();

        public void AddDataModelExtension(IDataModelExtension extension)
        {
            _dataModelExtensions.Add(extension);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            foreach (var extension in _dataModelExtensions)
            {
                Type type = extension.ResolveDataModelType(assemblyName, typeName);
                if (type != null) return type;
            }
            return base.BindToType(assemblyName, typeName);
        }
    }
}
