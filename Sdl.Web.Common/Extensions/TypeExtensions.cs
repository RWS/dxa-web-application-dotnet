using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common.Extensions
{
    public static class TypeExtensions
    {
        public static string BareTypeName(this Type type)
        {
           return type.Name.Split('`')[0]; // Type name without generic type parameters (if any)
        }
        
        public static bool IsGenericList(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }
        
        public static Type GetUnderlyingGenericListType(this Type type)
        {
            return type.IsGenericList() ? type.GetGenericArguments()[0] : null;
        }

        public static IList CreateGenericList(this Type listItemType)
        {
            ConstructorInfo genericListConstructor = typeof(List<>).MakeGenericType(listItemType).GetConstructor(Type.EmptyTypes);
            if (genericListConstructor == null)
            {
                // This should never happen.
                throw new DxaException($"Unable get constructor for generic list of '{listItemType.FullName}'.");
            }

            return (IList)genericListConstructor.Invoke(null);
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type GetUnderlyingNullableType(this Type type)
        {
            return type.IsNullable() ? type.GenericTypeArguments[0] : null;
        }

        public static object CreateInstance(this Type type, params object[] args)
        {
            return Activator.CreateInstance(type, args);
        }
    }
}
