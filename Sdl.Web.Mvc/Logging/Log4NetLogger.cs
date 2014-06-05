using log4net;
using log4net.Config;
using Sdl.Web.Mvc.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc
{
    public class Log4NetLogger : ILogger
    {
        private static bool _configured = false;

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
