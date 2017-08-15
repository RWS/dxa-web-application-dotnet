using System;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;

namespace DD4T.Mvc
{
    [Obsolete]
    public static class ServiceLocator
    {

        private static CompositionContainer container = null;
        private static AggregateCatalog catalog = null;
        private static string path = "bin";
        public static void Initialize()
        {
            catalog = new AggregateCatalog(new DirectoryCatalog(path));
            container = new CompositionContainer(catalog, true);
        }

        public static T GetInstance<T>()
        {
            return GetInstance<T>(null);
        }
        public static T GetInstance<T>(string overridePath)
        {
            if (! string.IsNullOrEmpty(overridePath)) path = overridePath;
            if (container == null) Initialize();

            try
            {
                return container.GetExportedValue<T>();
            }
            catch (ReflectionTypeLoadException e)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Exception le in e.LoaderExceptions)
                {
                    sb.AppendLine(le.Message);
                }
                throw new Exception(sb.ToString(), e);
            }

        }
    }
}
