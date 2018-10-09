using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Sdl.Web.Common.Logging
{
    /// <summary>
    /// Used for tracing method entry/exit calls.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Method entry trace is output when a <c>Tracer</c> instance is created.
    /// Method exit trace (including method duration) is output when the instance is disposed.
    /// In this manner, method entry/exit tracing can easily be achieved in code by wrapping 
    /// the method's entire implementation in a <c>using (new Tracer())</c> statement.
    /// </para>
    /// <example><code>
    /// void TracerTest()
    /// {
    ///   using (new Tracer())
    ///   {
    ///     // Entire method implementation goes here.
    ///   }
    /// }
    /// </code></example>
    /// </remarks>
    public class Tracer : IDisposable
    {
        private static readonly bool _isTracingEnabled;
        //private static readonly CommonSettings _commonConfigSection;

        [ThreadStatic]
        private static int _nestingLevel;

        private string _indent;
        private string _callingMethodName;
        private int _numberOfCallingMethodParams;
        private Stopwatch _stopwatch = new Stopwatch();

        #region Constructors
        /// <summary>
        /// Initializes static members of the <see cref="Tracer"/> class. 
        /// </summary>
        static Tracer()
        {
            // We store the tracing enabled setting in a static to minimize the performance overhead of tracing when it is disabled.
            _isTracingEnabled = Log.Logger.IsTracingEnabled;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class. 
        /// Results in a method entry trace being output.
        /// </summary>
        public Tracer()
        {
            if (!_isTracingEnabled)
            {
                return;
            }

            // Due to optimizations during JIT compilation the StackTrace and its properties are not reliable 
            // especially when the following code is in a separate method
            // http://www.smelser.net/blog/2008/11/default.aspx
            _numberOfCallingMethodParams = 0;
            _callingMethodName = "Not available";

            StackTrace stackTrace = new StackTrace();
            StackFrame callingMethodFrame = stackTrace.GetFrame(1);

            if (callingMethodFrame != null)
            {
                MethodBase methodInfo = callingMethodFrame.GetMethod();
                if (methodInfo != null)
                {
                    ParameterInfo[] parameterInfo = methodInfo.GetParameters();
                    _numberOfCallingMethodParams = parameterInfo.Count();

                    Type declaringType = methodInfo.DeclaringType;
                    if (declaringType != null)
                    {
                        _callingMethodName = declaringType.Name + "." + methodInfo.Name;
                    }
                }
            }

            TraceMethodEntry(null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tracer"/> class using given method parameter values.
        /// Results in a method entry trace being output.
        /// </summary>
        /// <param name="parameters">The method parameter values to include in the trace.</param>
        /// <remarks>
        /// For optimal performance when tracing is disabled, perform as little as possible operations to get the parameter values.
        /// If you have a parameter of a type which implements <see cref="Object.ToString()"/>, don't call this method yourself;
        /// the Tracer implementation will call it only when tracing is enabled.
        /// </remarks>
        public Tracer(params object[] parameters)
        {
            if (!_isTracingEnabled)
            {
                return;
            }

            // Due to optimizations during JIT compilation the StackTrace and its properties are not reliable 
            // especially when the following code is in a separate method
            // http://www.smelser.net/blog/2008/11/default.aspx
            _numberOfCallingMethodParams = 0;
            _callingMethodName = "Not available";

            StackTrace stackTrace = new StackTrace();
            StackFrame callingMethodFrame = stackTrace.GetFrame(1);

            if (callingMethodFrame != null)
            {
                MethodBase methodInfo = callingMethodFrame.GetMethod();
                if (methodInfo != null)
                {
                    ParameterInfo[] parameterInfo = methodInfo.GetParameters();
                    _numberOfCallingMethodParams = parameterInfo.Count();

                    Type declaringType = methodInfo.DeclaringType;
                    if (declaringType != null)
                    {
                        _callingMethodName = declaringType.Name + "." + methodInfo.Name;
                    }
                }
            }

            TraceMethodEntry(parameters);
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Disposes the instance. Results in a method exit trace being output.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (!_isTracingEnabled)
            {
                return;
            }

            TraceMethodExit();

            _stopwatch = null;
        }
        #endregion

        #region Internals
        /// <summary>
        /// Outputs a method entry trace.
        /// </summary>
        /// <param name="parameters">The parameter values to include in the trace.</param>
        private void TraceMethodEntry(object[] parameters)
        {
            _indent = new string(' ', _nestingLevel * 2);
            _nestingLevel++;

            string parametersString;
            string extraInfo = string.Empty;
            if (parameters == null)
            {
                parametersString = null;
            }
            else
            {
                string[] parameterStrings = parameters.Select(p => ConvertParameterToString(p)).ToArray();
                parametersString = string.Join(", ", parameterStrings.Take(_numberOfCallingMethodParams).ToArray());
                extraInfo = string.Join(", ", parameterStrings.Skip(_numberOfCallingMethodParams).ToArray());
            }

            string traceMessage = string.Format(
                "{0}{1}({2}){3} entry.",
                _indent,
                _callingMethodName,
                parametersString,
                string.IsNullOrEmpty(extraInfo) ? string.Empty : " : " + extraInfo
                );
          
            Log.Trace(traceMessage);

            _stopwatch.Start();
        }

        /// <summary>
        /// Outputs a method exit trace.
        /// </summary>
        private void TraceMethodExit()
        {
            _stopwatch.Stop();

            string traceMessage = string.Format(
                "{0}{1}() exit. Duration: {2} ms.",
                _indent,
                _callingMethodName,
                _stopwatch.ElapsedMilliseconds
                );
            
            Log.Trace(traceMessage);

            _nestingLevel--;
        }

        /// <summary>
        /// Converts the parameter supplied to the Tracer to a string and limit the value to the in configuration defined number of characters.
        /// </summary>
        /// <param name="param">The parameter to convert.</param>
        /// <returns>A string that contains the value of the supplied parameter.</returns>
        internal static string ConvertParameterToString(object param)
        {
            if (param == null)
            {
                return "null";
            }

            // The resultlimit for truncation can have a default value for all types. If it is not set, no truncation should be done.
            int resultLimit = 64; // TODO: _commonConfigSection.TracingSettings.ParameterValueTruncation.Default;
            //ParameterTypeElement element = (from ParameterTypeElement item in _commonConfigSection.TracingSettings.ParameterValueTruncation.ParameterTypes
            //                                where Type.GetType(item.Type + (String.IsNullOrEmpty(item.Assembly) ? String.Empty : ", " + item.Assembly)).IsAssignableFrom(param.GetType())
            //                                select item).FirstOrDefault();
            //// The resultlimit for truncation can also be set per item. If the item is defined, use the maxLength value. If there is no maxLength defined, no truncation should be done.
            //if (element != null)
            //{
            //    resultLimit = element.MaxLength;
            //}

            //if (resultLimit == 0)
            //{
            //    return "…";
            //}

            bool isConverted = false;
            string result = param.ToString();

            if (param is string)
            {
                result = "\"" + param + "\"";
                isConverted = true;
            }

            if (param is XmlNode)
            {
                result = ((XmlNode)param).OuterXml;
                isConverted = true;
            }

            // Almost everything is a IEnumerable
            // First test if we already converted the parameter
            if (!isConverted && param is IEnumerable)
            {
                string[] itemStrings = ((IEnumerable)param).Cast<object>().Select(p => ConvertParameterToString(p)).ToArray();
                result = string.Join("; ", itemStrings);
            }

            // Limit the result (only when there is a result limit defined)
            if (result != null)
            {
                int length = result.Length;
                if (length > resultLimit && resultLimit != -1)
                {
                    result = result.Substring(0, resultLimit);
                    result += "…";

                    if (result.StartsWith("\"") && !result.EndsWith("\""))
                    {
                        result += "\"";
                    }
                }
            }
            return result;
        }

        #endregion
    }
}
