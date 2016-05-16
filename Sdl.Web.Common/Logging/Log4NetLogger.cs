using System;
using System.Web;
using log4net;
using log4net.Config;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Logging
{
    public class Log4NetLogger : ILogger
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(Log4NetLogger));

        private const string TraceFormat = "url:{0},type:{1},time:{2},details:{3}";

        #region ILogger members

        public void Trace(string messageFormat, params object[] parameters)
        {
            ILog log = _log;
            if (IsTracingEnabled)
            {
                // no trace output available in log4net so we use debug instead but we should move to
                // using the same logging as the CIL library (trace listeners) and then we can remove 
                // the log4net dependency from DXA and instead provide a DXA logging module that will 
                // allow people to use it if they wish by implementing a trace listener to forward on
                // log writes to log4net.
                log.DebugFormat(messageFormat, parameters);   
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
                return _log.IsDebugEnabled;
            }
        }
        #endregion
    }
}
