using System;
using Sdl.Web.Common.Configuration;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Logging
{
    /// <summary>
    /// Log class used by framework to generate log information. This class will use whatever ILogger implementation
    /// is provided through unity and default to using the Log4Net implementation if no logger is configured.
    /// </summary>
    public static class Log
    {
        private static ILogger _logger;

        public static ILogger Logger
        {
            get
            {
                ILogger configuredLogger = SiteConfiguration.Logger;
                if (configuredLogger != null)
                {
                    return configuredLogger;
                }

                // No Logger configured/initialized yet
                return _logger ?? (_logger = new Log4NetLogger());
            }
            set
            {
                _logger = value;
            }
        }

        public static bool IsDebugEnabled => Logger.IsDebugEnabled;

        public static bool IsTraceEnabled => Logger.IsTracingEnabled;

        public static void Trace(string messageFormat, params object[] parameters) => Logger.Trace(messageFormat, parameters);

        public static void Debug(string messageFormat, params object[] parameters) => Logger.Debug(messageFormat, parameters);

        public static void Info(string messageFormat, params object[] parameters) => Logger.Info(messageFormat, parameters);

        public static void Warn(string messageFormat, params object[] parameters) => Logger.Warn(messageFormat, parameters);

        public static void Error(string messageFormat, params object[] parameters) => Logger.Error(messageFormat, parameters);

        public static void Error(Exception ex, string messageFormat, params object[] parameters) => Logger.Error(ex, messageFormat, parameters);

        public static void Error(Exception ex) => Logger.Error(ex);
    }
}
