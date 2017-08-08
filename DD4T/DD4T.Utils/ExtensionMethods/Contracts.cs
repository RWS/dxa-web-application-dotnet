using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DD4T.Utils
{
    internal static class Contract
    {
        public static void ThrowIfNull<T>(T value, string message = null) where T : class
        {
            if (value == null)
            {
                message = message ?? "Unexpected Null";
                throw new ArgumentNullException(message);
            }
        }

        public static void ThrowIfNull<T>(T value, string message, string parameterName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(message, parameterName);
            }
        }
    }
  
}
