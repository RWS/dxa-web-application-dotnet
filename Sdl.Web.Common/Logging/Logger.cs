using System;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Logging
{
    public static class Log
    {
        private static ILogger _logger;
        public static ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = new Log4NetLogger();
                    Log4NetLogger.Configure();
                }
                return _logger;
            }
            set
            {
                _logger = value;
            }
        }

        public static void Trace(DateTime start, string type, string messageFormat, params object[] parameters)
        {
            Logger.Trace(start, type, messageFormat, parameters);
        }

        public static void Debug(string messageFormat, params object[] parameters)
        {
            Logger.Debug(messageFormat, parameters);
        }

        public static void Info(string messageFormat, params object[] parameters)
        {
            Logger.Info(messageFormat, parameters);
        }

        public static void Warn(string messageFormat, params object[] parameters)
        {
            Logger.Warn(messageFormat, parameters);
        }

        public static void Error(string messageFormat, params object[] parameters)
        {
            Logger.Error(messageFormat, parameters);
        }

        public static void Error(Exception ex, string messageFormat, params object[] parameters)
        {
            Logger.Error(ex, messageFormat, parameters);
        }

        public static void Error(Exception ex)
        {
            Logger.Error(ex);
        }
    }
}
