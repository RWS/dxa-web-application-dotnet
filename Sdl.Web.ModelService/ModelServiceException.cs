using System;

namespace Sdl.Web.ModelService
{
    public class ModelServiceException : Exception
    {
        public ModelServiceException(string message) : base(message)
        {
        }

        public ModelServiceException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}
