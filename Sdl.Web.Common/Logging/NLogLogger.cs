using System;
using NLog;

namespace Sdl.Web.Common.Logging
{
    /// <summary>
    /// NLog4 implementation.
    /// </summary>
    public class NLogLogger : Sdl.Web.Common.Interfaces.ILogger
    {
        private static readonly NLog.ILogger Log = LogManager.GetCurrentClassLogger();

        #region ILogger members

        public void Trace(string messageFormat, params object[] parameters)
        {
            if (!IsTracingEnabled) return;
            Log.Trace(messageFormat, parameters);
        }

        public void Debug(string messageFormat, params object[] parameters)
        {
            if (!Log.IsDebugEnabled) return;
            Log.Debug(messageFormat, parameters);
        }

        public void Info(string messageFormat, params object[] parameters)
        {
            if (!Log.IsInfoEnabled) return;
            Log.Info(messageFormat, parameters);
        }

        public void Warn(string messageFormat, params object[] parameters)
        {
            if (!Log.IsWarnEnabled) return;
            Log.Warn(messageFormat, parameters);
        }

        public void Error(string messageFormat, params object[] parameters)
        {
            if (!Log.IsErrorEnabled) return;
            Log.Error(messageFormat, parameters);
        }

        public void Error(Exception ex, string messageFormat, params object[] parameters)
        {
            if (!Log.IsErrorEnabled) return;
            Log.Error(ex, messageFormat, parameters);
        }

        public void Error(Exception ex)
        {
            if (!Log.IsErrorEnabled) return;
            Log.Error(ex,ex.ToString());
        }

        public bool IsTracingEnabled => Log.IsTraceEnabled;

        public bool IsDebugEnabled => Log.IsDebugEnabled;

        #endregion
    }
}
