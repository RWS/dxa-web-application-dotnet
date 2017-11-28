using System;
using Sdl.Web.Common;

namespace Sdl.Web.Tridion.ContentManager
{
    /// <summary>
    /// The exception that is thrown for conversion of invalid <see cref="String"/> to <see cref="TcmUri "/>.
    /// </summary>
    /// <remarks>
    /// This Exception is thrown : 
    /// <list type="bullet">
    /// <item> while instantiating a <see cref="TcmUri"/> using  an invalid <see cref="String"/> representation, if explicitly mentioned. </item>
    /// <item> while deserializing an invalid <see cref="String"/> representation of <see cref="TcmUri "/> to <see cref="TcmUri "/>. </item>
    /// </list>
    /// </remarks>
    public class InvalidTcmUriException : DxaException
    {
        /// <summary>
        /// Initializes a new instance of the InvalidTcmUriException class with a predefined error message.
        /// </summary>
        /// <param name="uri">
        /// The string representation of a <see cref="TcmUri"/>.
        /// </param>
        public InvalidTcmUriException(string uri)
            : base("Invalid TCM URI: " + uri)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidTcmUriException class with a predefined error message and inner exception.
        /// </summary>
        /// <param name="uri">
        /// The string representation of a <see cref="TcmUri"/>.
        /// </param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the <c>innerException</c> parameter is not a <c>null</c> reference, the current exception is raised in a catch block that handles the inner exception.
        /// </param>
        public InvalidTcmUriException(string uri, Exception innerException)
            : base("Invalid TCM URI: " + uri, innerException)
        {
        }
    }
}
