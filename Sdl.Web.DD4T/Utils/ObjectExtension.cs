using System.Collections.Generic;
using System.Linq;

namespace System
{
    public static class ObjectExtension
    {
        public static IEnumerable<OUTPUT> IfNotNull<INPUT, OUTPUT>(this INPUT value, Func<INPUT, IEnumerable<OUTPUT>> getResult)
        {
            return null != value ? getResult(value) : Enumerable.Empty<OUTPUT>();
        }

        public static OUTPUT IfNotNull<INPUT, OUTPUT>(this INPUT value, Func<INPUT, OUTPUT> getResult)
        {
            return null != value ? getResult(value) : default(OUTPUT);
        }
        
        public static void IfNotNull<INPUT>(this INPUT value, Action<INPUT> action)
        {
            if(null != value)
            {
                action(value);
            }
        }
    }
}