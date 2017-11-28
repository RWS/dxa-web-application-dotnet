using System;

namespace Sdl.Web.Common
{
    /// <summary>
    /// Base class for exceptions thrown by DXA framework code.
    /// </summary>
    [Serializable]
    public class DxaException : ApplicationException
    {
        public DxaException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
