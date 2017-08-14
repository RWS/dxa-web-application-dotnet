using System;
using System.Reflection;

namespace DD4T.Utils
{
    public static class ClassLoader
    {
        public static object Load<T>(string assemblyName) where T : class
        {
            Assembly a = Assembly.Load(assemblyName);
            return Load<T>(a);
            //Type type = Type.GetType(className);
            //object o = Activator.CreateInstance(type) as T;
            //return o;
        }
        public static object Load<T>(Assembly assembly) where T : class
        {
            // Walk through each type in the assembly looking for our class
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsClass == true)
                {
                    if (typeof(T).IsAssignableFrom(type))
                    {
                        // create an instance of the object
                        return Activator.CreateInstance(type);
                    }
                }
            }
            //LoggerService.Warning("could not find type {0} in assembly {1}", typeof(T).FullName, assembly.FullName);
            return null;
        }
    }
}
