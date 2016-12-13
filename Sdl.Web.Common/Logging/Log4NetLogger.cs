using System;
using System.Diagnostics;
using System.Globalization;
using System.Web;
using log4net;
using log4net.Config;
using log4net.Util;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Logging
{
    public class Log4NetLogger : ILogger
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Log4NetLogger));

        private const string TraceFormat = "url:{0},type:{1},time:{2},details:{3}";

        public Log4NetLogger()
        {
            Configure();
        }

        #region ILogger members

        public void Trace(string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (IsTracingEnabled)
            {          
                // the log4net wrapper doesn't actually have a Trace method so instead we implement our own. we are lucky because log4net DOES include
                // a trace level in its enum :-)
                System.Reflection.MethodBase mb = new StackFrame(3).GetMethod();
                log.Logger.Log(mb.DeclaringType, log4net.Core.Level.Trace, new SystemStringFormat(CultureInfo.InvariantCulture, messageFormat, parameters), null);
            }
        }

        public void Debug(string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (log.IsDebugEnabled)
            {
                log.DebugFormat(messageFormat, parameters);
            }
        }

        public void Info(string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (log.IsInfoEnabled)
            {
                log.InfoFormat(messageFormat, parameters);
            }
        }

        public void Warn(string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (log.IsWarnEnabled)
            {
                log.WarnFormat(messageFormat, parameters);
            }
        }

        public void Error(string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (log.IsErrorEnabled)
            {
                log.ErrorFormat(messageFormat, parameters);
            }
        }

        public void Error(Exception ex, string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (log.IsErrorEnabled)
            {
                log.Error(String.Format(messageFormat,parameters),ex);
            }
        }

        public void Error(Exception ex)
        {
            ILog log = _log;            
            if (log.IsErrorEnabled)
            {
                log.Error(ex.Message, ex);
            }
        }

        public static void Configure()
        {
            XmlConfigurator.Configure();
        }

        public bool IsTracingEnabled
        {
            get
            {
                return _log.Logger.IsEnabledFor(log4net.Core.Level.Trace);
            }
        }

        public bool IsDebugEnabled
        {
            get
            {
                return _log.IsDebugEnabled;
            }
        }
        #endregion
    }
}
