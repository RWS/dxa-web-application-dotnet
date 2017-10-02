using System;

namespace DD4T.Providers.DxaModelService.Caching
{
    public class DxaCacheAgentException : Exception
    {
        public DxaCacheAgentException(string msg) : base(msg) { }
        public DxaCacheAgentException(string msg, Exception innerException) : base(msg, innerException) { }
    }
}
