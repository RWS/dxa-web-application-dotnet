using DD4T.ContentModel.Contracts.Logging;
using NLog;

namespace DD4T.Logging.NLog
{
    public class NLog : DD4T.ContentModel.Contracts.Logging.ILogger
    {
        private Logger logger = LogManager.GetCurrentClassLogger();

        //public NLog(Logger logger)
        //{
        //    this.logger = logger;
        //}

        public void Critical(string message, LoggingCategory category, params object[] parameters)
        {
            logger.Error(message, parameters);
        }

        public void Critical(string message, params object[] parameters)
        {
            logger.Error(message, parameters);
        }

        public void Debug(string message, LoggingCategory category, params object[] parameters)
        {
            logger.Debug(message, parameters);
        }

        public void Debug(string message, params object[] parameters)
        {
            logger.Debug(message, parameters);
        }

        public void Error(string message, LoggingCategory category, params object[] parameters)
        {
            logger.Error(message, parameters);
        }

        public void Error(string message, params object[] parameters)
        {
            logger.Error(message, parameters);
        }

        public void Information(string message, LoggingCategory category, params object[] parameters)
        {
            logger.Info(message, parameters);
        }

        public void Information(string message, params object[] parameters)
        {
            logger.Info(message, parameters);
        }

        public void Warning(string message, LoggingCategory category, params object[] parameters)
        {
            logger.Warn(message, parameters);
        }

        public void Warning(string message, params object[] parameters)
        {
            logger.Warn(message, parameters);
        }
    }
}
