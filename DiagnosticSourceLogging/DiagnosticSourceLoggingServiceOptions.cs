using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace DiagnosticSourceLogging
{
    public struct FormatterArg
    {
        public FormatterArg(string sourceName, string eventName, object arg)
        {
            SourceName = sourceName;
            EventName = eventName;
            Arg = arg;
        }
        public string SourceName { get; }
        public string EventName { get; }
        public object Arg { get; }
    }
    public interface IDiagnosticSourceLoggingServiceOptions
    {
        /// <summary>determine whether subscribing or not for DiagnosticListener</summary>
        /// <remarks>Mostly, this will be called much less times than IsEnabled</remarks>
        bool ShouldListen(DiagnosticListener listener);

        /// <summary>determine watching individual diagnostic event</summary>
        /// <param name="sourceName">equals to DiagnosticSource.Name</param>
        /// <param name="eventName">equals to DiagnosticSource.IsEnabled 1st argument</param>
        /// <param name="arg1">equals to DiagnosticSource.IsEnabled 2nd argument</param>
        /// <param name="arg2">equals to DiagnosticSource.IsEnabled 3rd argument</param>
        /// <remarks>This may be called many times</remarks>
        bool IsEnabled(string sourceName, string eventName, object arg1, object arg2);
        string GetFormattedString(string sourceName, string eventName, object arg);
        LogLevel GetLogLevel(string sourceName, string eventName);
        EventId GetEventId(string sourceName, string eventName);
        string Formatter(string sourceName, KeyValuePair<string, object> kv, Exception exception);
    }
}