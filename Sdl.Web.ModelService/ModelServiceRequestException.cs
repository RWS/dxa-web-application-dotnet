using System;

namespace Sdl.Web.ModelService
{
    public class ModelServiceRequestException : Exception
    {
        public string ResponseBody { get; }

        public ModelServiceRequestException(string response)
        {
            ResponseBody = response;
        }
    }
}
