using DD4T.ContentModel.Contracts.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.Utils.Logging
{
    public class NullLogger : ILogger
    {
        public void Debug(string message, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Debug(string message, LoggingCategory category, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Information(string message, params object[] parameters)
        {
           // throw new NotImplementedException();
        }

        public void Information(string message, LoggingCategory category, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Warning(string message, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Warning(string message, LoggingCategory category, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Error(string message, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Error(string message, LoggingCategory category, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Critical(string message, params object[] parameters)
        {
            //throw new NotImplementedException();
        }

        public void Critical(string message, LoggingCategory category, params object[] parameters)
        {
            //throw new NotImplementedException();
        }
    }
}
