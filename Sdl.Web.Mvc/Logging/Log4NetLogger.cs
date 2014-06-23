using log4net;
using log4net.Config;
using Sdl.Web.Mvc.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sdl.Web.Mvc
{
    public class Log4NetLogger : ILogger
    {
        private static bool _configured = false;
        private static string traceFormat = "url:{0},type:{1},time:{2},details:{3}";
        
        /// <summary>
        /// Used to log performance metrics to a separate log file
        /// </summary>
        /// <param name="time">Time (in milliseconds) to execute the action</param>
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
                catch (Exception ex)
                {
                    //ignore - we are in a non request context
                }
                var message = String.Format(messageFormat, parameters);
                log.InfoFormat(traceFormat, url, type, (DateTime.Now - start).TotalMilliseconds, message);
            }
        }

        public void Debug(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsDebugEnabled)
                log.DebugFormat(messageFormat, parameters);
        }

        public void Info(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsInfoEnabled)
                log.InfoFormat(messageFormat, parameters);
        }

        public void Warn(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsWarnEnabled)
                log.WarnFormat(messageFormat, parameters);
        }

        public void Error(string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsErrorEnabled)
                log.ErrorFormat(messageFormat, parameters);
        }

        public void Error(Exception ex, string messageFormat, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsErrorEnabled)
                log.Error(String.Format(messageFormat,parameters),ex);
        }

        public void Error(Exception ex)
        {
            ILog log = LogManager.GetLogger(typeof(Log4NetLogger));
            if (log.IsErrorEnabled)
                log.Error(ex.Message, ex);
        }

        public static void Configure()
        {
            if (!_configured)
            {
                XmlConfigurator.Configure();
            }
        }
    }
}
