using System;
using System.Collections.Generic;
using System.Linq;

namespace Sdl.Web.Tridion.Utils
{
    public static class ObjectExtension
    {
        public static IEnumerable<TOutput> IfNotNull<TInput, TOutput>(this TInput value, Func<TInput, IEnumerable<TOutput>> getResult)
            where TInput: class
        {
            return null != value ? getResult(value) : Enumerable.Empty<TOutput>();
        }

        public static TOutput IfNotNull<TInput, TOutput>(this TInput value, Func<TInput, TOutput> getResult)
            where TInput: class
        {
            return null != value ? getResult(value) : default(TOutput);
        }
        
        public static void IfNotNull<T>(this T value, Action<T> action)
            where T: class
        {
            if (null != value)
            {
                action(value);
            }
        }
    }
}