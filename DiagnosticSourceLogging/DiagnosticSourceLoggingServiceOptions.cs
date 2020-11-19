using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

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
        Func<DiagnosticListener, bool> ShouldListen { get; }
        Func<string, string, object, object, bool> IsEnabled { get; }
        Func<FormatterArg, Exception, string> Formatter { get; }
        Func<string, string, LogLevel> LogLevelGetter { get; }
    }
}