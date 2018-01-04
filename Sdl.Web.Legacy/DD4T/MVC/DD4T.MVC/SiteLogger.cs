using System.Diagnostics;
//using Microsoft.Practices.EnterpriseLibrary.Logging;

namespace DD4T.Utils
{
    public enum TraceMode { Start, Finish, None }

    public enum LoggingCategory { General, Controller, View, Model, Search, Integration, Performance }

    public static class Logger
    {
        public static void Debug(string message, params string[] parameters)
        {
            Log(message, LoggingCategory.General, TraceEventType.Verbose, parameters);
        }

        public static void Debug(string message, LoggingCategory category, params string[] parameters)
        {
            Log(message, category, TraceEventType.Verbose, parameters);
        }

        public static void Information(string message, params string[] parameters)
        {
            Log(message, LoggingCategory.General, TraceEventType.Information, parameters);
        }

        public static void Information(string message, LoggingCategory category, params string[] parameters)
        {
            Log(message, category, TraceEventType.Information, parameters);
        }

        public static void Warning(string message, params string[] parameters)
        {
            Log(message, LoggingCategory.General, TraceEventType.Warning, parameters);
        }

        public static void Warning(string message, LoggingCategory category, params string[] parameters)
        {
            Log(message, category, TraceEventType.Warning, parameters);
        }

        public static void Error(string message, params string[] parameters)
        {
            Log(message, LoggingCategory.General, TraceEventType.Error, parameters);
        }

        public static void Error(string message, LoggingCategory category, params string[] parameters)
        {
            Log(message, category, TraceEventType.Error, parameters);
        }

        public static void Critical(string message, params string[] parameters)
        {
            Log(message, LoggingCategory.General, TraceEventType.Critical, parameters);
        }

        public static void Critical(string message, LoggingCategory category, params string[] parameters)
        {
            Log(message, category, TraceEventType.Critical, parameters);
        }

        private static void Log(string message, LoggingCategory category, TraceEventType severity,
                                params string[] parameters)
        {
            //var logEntry = new LogEntry();
            //logEntry.Categories.Add(category.ToString());
            //logEntry.Severity = severity;
            //if (Logger.ShouldLog(logEntry))
            //{
            //    if (parameters.Length > 0)
            //    {
            //        logEntry.Message = string.Format(message, parameters);
            //    }
            //    else
            //    {
            //        logEntry.Message = message;
            //    }
            //}
            //Logger.Write(logEntry);
        }
    }
}