using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace DiagnosticSourceLogging
{
    public record FormatterArg(string SourceName, string EventName, object Arg);
    // {
    //     public string SourceName { get; set; }
    //     public string EventName { get; set; }
    //     public object Arg { get; set; }
    // }
    public interface IDiagnosticSourceLoggingServiceOptions
    {
        Func<DiagnosticListener, bool> ShouldListen { get; }
        Func<string, object, object, bool> IsEnabled { get; }
        Func<FormatterArg, Exception, string> Formatter { get; }
        Func<string, string, LogLevel> LogLevelGetter { get; }
    }
}