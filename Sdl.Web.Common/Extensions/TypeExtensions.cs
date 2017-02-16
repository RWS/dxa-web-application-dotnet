using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
