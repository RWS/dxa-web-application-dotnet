using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Common
{
    /// <summary>
    /// Base class for exceptions thrown by DXA framework code.
    /// </summary>
    public class DxaException : ApplicationException
    {
        public DxaException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
