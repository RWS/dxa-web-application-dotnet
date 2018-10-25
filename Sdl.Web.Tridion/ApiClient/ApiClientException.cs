using System;

namespace Sdl.Web.Tridion.ApiClient
{
    public class ApiClientException : Exception
    {
        public ApiClientException(string message) : base(message)
        {
        }
    }
}
