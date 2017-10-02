namespace DD4T.ContentModel.Exceptions
{
    using System;

    [Serializable]
    public class ConfigurationException : ApplicationException
    {
        public ConfigurationException()
            : base()
        {
        }
        public ConfigurationException(string message)
            : base(message)
        {
        }
        public ConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
