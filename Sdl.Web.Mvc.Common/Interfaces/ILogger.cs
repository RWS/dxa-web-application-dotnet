using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sdl.Web.Mvc.Common
{
    public interface ILogger
    {
        void Trace(DateTime start, string type, string messageFormat, params object[] parameters);
        void Debug(string messageFormat, params object[] parameters);
        void Info(string messageFormat, params object[] parameters);
        void Warn(string messageFormat, params object[] parameters);
        void Error(string messageFormat, params object[] parameters);
        void Error(Exception ex, string messageFormat, params object[] parameters);
        void Error(Exception ex);
    }
}
