using DD4T.ContentModel.Contracts.Logging;
using log4net;


namespace DD4T.Logging.Log4net
{ 
   public class DefaultLogger : ILogger
    {
        public void Critical(string message, LoggingCategory category, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(category.ToString());
            //Any idea what to do with the category?
            log.FatalFormat(message, parameters);
        }

        public void Critical(string message, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(DefaultLogger));
            log.FatalFormat(message, parameters);
        }

        public void Debug(string message, LoggingCategory category, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(category.ToString());
            if (log.IsDebugEnabled)
                log.DebugFormat(message, parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(DefaultLogger));
            if (log.IsDebugEnabled)
                log.DebugFormat(message, parameters);
        }

        public void Error(string message, LoggingCategory category, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(category.ToString());
            log.ErrorFormat(message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(DefaultLogger));
            log.ErrorFormat(message, parameters);
        }

        public void Information(string message, LoggingCategory category, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(category.ToString());
            log.InfoFormat(message, parameters);
        }

        public void Information(string message, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(DefaultLogger));
            log.InfoFormat(message, parameters);
        }

        public void Warning(string message, LoggingCategory category, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(category.ToString());
            log.WarnFormat(message, parameters);
        }

        public void Warning(string message, params object[] parameters)
        {
            ILog log = LogManager.GetLogger(typeof(DefaultLogger));
            log.WarnFormat(message, parameters);
        }
    }
}
