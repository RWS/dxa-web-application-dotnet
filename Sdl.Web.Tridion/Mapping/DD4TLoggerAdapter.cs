using DD4T.ContentModel.Contracts.Logging;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion.Mapping
{
    /// <summary>
    /// Adapter class which exposes the DXA Logger as DD4T Logger.
    /// </summary>
    internal class DD4TLoggerAdapter : ILogger
    {
        public void Debug(string message, params object[] parameters)
        {
            Log.Debug(message, parameters);
        }

        public void Debug(string message, LoggingCategory category, params object[] parameters)
        {
            if (category == LoggingCategory.Performance)
            {
                Log.Trace(message, parameters);
            }
            else
            {
                Log.Debug(message, parameters);
            }
        }

        public void Information(string message, params object[] parameters)
        {
            Log.Info(message, parameters);
        }

        public void Information(string message, LoggingCategory category, params object[] parameters)
        {
            Log.Info(message, parameters);
        }

        public void Warning(string message, params object[] parameters)
        {
            Log.Warn(message, parameters);
        }

        public void Warning(string message, LoggingCategory category, params object[] parameters)
        {
            Log.Warn(message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            Log.Error(message, parameters);
        }

        public void Error(string message, LoggingCategory category, params object[] parameters)
        {
            Log.Error(message, parameters);
        }

        public void Critical(string message, params object[] parameters)
        {
            Log.Error(message, parameters);
        }

        public void Critical(string message, LoggingCategory category, params object[] parameters)
        {
            Log.Error(message, parameters);
        }
    }
}
