using System;
using System.Web;
using log4net;
using log4net.Config;
using Sdl.Web.Common.Interfaces;

namespace Sdl.Web.Common.Logging
{
    public class Log4NetLogger : ILogger
    {
        //private const bool Configured = false;
        private const string TraceFormat = "url:{0},type:{1},time:{2},details:{3}";

        /// <summary>
        /// Used to log performance metrics to a separate log file
        /// </summary>
        /// <param name="start">Date and time to execute the action</param>
        /// <param name="type">Type of action</param>
        /// <param name="messageFormat">Detailed message format string</param>
        /// <param name="parameters">Message format string parameters</param>
        public void Trace(DateTime start, string type, string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger("Trace");
            if (log.IsInfoEnabled)
            {
                var url = "[none]";
                try
                {
                    url = HttpContext.Current.Request.RawUrl;
                }
                catch (Exception)
                {
                    //ignore - we are in a non request context
                }
                var message = String.Format(messageFormat ?? "", parameters);
                log.InfoFormat(TraceFormat, url, type, (DateTime.Now - start).TotalMilliseconds, message);
            }
        }

        public void Debug(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsDebugEnabled)
            {
                log.DebugFormat(messageFormat, parameters);
            }
        }

        public void Info(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsInfoEnabled)
            {
                log.InfoFormat(messageFormat, parameters);
            }
        }

        public void Warn(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsWarnEnabled)
            {
                log.WarnFormat(messageFormat, parameters);
            }
        }

        public void Error(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsErrorEnabled)
            {
                log.ErrorFormat(messageFormat, parameters);
            }
        }

        public void Error(Exception ex, string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsErrorEnabled)
            {
                log.Error(String.Format(messageFormat,parameters),ex);
            }
        }

        public void Error(Exception ex)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsErrorEnabled)
            {
                log.Error(ex.Message, ex);
            }
        }

        public static void Configure()
        {
            XmlConfigurator.Configure();
        }
    }
}
