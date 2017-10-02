using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Sdl.Web.Common.Extensions
{
    public static class TypeExtensions
    {
        public static string BareTypeName(this Type type) => type.Name.Split('`')[0];

        public static bool IsGenericList(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);

        public static Type GetUnderlyingGenericListType(this Type type) => type.IsGenericList() ? type.GetGenericArguments()[0] : null;

        public static IList CreateGenericList(this Type listItemType)
        {
            ConstructorInfo genericListConstructor = typeof(List<>).MakeGenericType(listItemType).GetConstructor(Type.EmptyTypes);
            if (genericListConstructor != null) return (IList) genericListConstructor.Invoke(null);
            // This should never happen.
            throw new DxaException($"Unable get constructor for generic list of '{listItemType.FullName}'.");
        }

        public static bool IsNullable(this Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static Type GetUnderlyingNullableType(this Type type) => type.IsNullable() ? type.GenericTypeArguments[0] : null;

        public static object CreateInstance(this Type type, params object[] args) => Activator.CreateInstance(type, args);
    }
}
