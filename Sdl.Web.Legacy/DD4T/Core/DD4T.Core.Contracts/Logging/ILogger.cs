using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DD4T.ContentModel.Contracts.Logging
{
    public enum LoggingCategory { General, Background, Controller, View, Model, System, Integration, Performance }
    public interface ILogger
    {
        void Debug(string message, params object[] parameters);
        void Debug(string message, LoggingCategory category, params object[] parameters);
        void Information(string message, params object[] parameters);
        void Information(string message, LoggingCategory category, params object[] parameters);
        void Warning(string message, params object[] parameters);
        void Warning(string message, LoggingCategory category, params object[] parameters);
        void Error(string message, params object[] parameters);
        void Error(string message, LoggingCategory category, params object[] parameters);
        void Critical(string message, params object[] parameters);
        void Critical(string message, LoggingCategory category, params object[] parameters);
    }
}
