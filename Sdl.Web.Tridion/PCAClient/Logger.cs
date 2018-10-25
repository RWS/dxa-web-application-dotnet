using System;
using Sdl.Tridion.Api.Client.Core;
using Sdl.Web.Common.Logging;

namespace Sdl.Web.Tridion.PCAClient
{
    /// <summary>
    /// Log implementation used by the PCA client just forwards on logging to be handled by the DXA
    /// log implementation that can be switched through Unity if required.
    /// </summary>
    public class Logger : ILogger
    {
        protected string Reformat(string msg)
        {
            return msg.Replace("{", "{{").Replace("}", "}}");
        }

        public void Trace(string messageFormat, params object[] parameters) => Log.Trace(Reformat(messageFormat), parameters);
        public void Debug(string messageFormat, params object[] parameters) => Log.Debug(Reformat(messageFormat), parameters);
        public void Info(string messageFormat, params object[] parameters) => Log.Info(Reformat(messageFormat), parameters);
        public void Warn(string messageFormat, params object[] parameters) => Log.Warn(Reformat(messageFormat), parameters);
        public void Error(string messageFormat, params object[] parameters) => Log.Error(Reformat(messageFormat), parameters);
        public void Error(Exception ex, string messageFormat, params object[] parameters) => Log.Error(ex, Reformat(messageFormat), parameters);
        public void Error(Exception ex) => Log.Error(ex);
        public bool IsTracingEnabled => Log.IsTraceEnabled;
        public bool IsDebugEnabled => Log.IsDebugEnabled;
    }
}
