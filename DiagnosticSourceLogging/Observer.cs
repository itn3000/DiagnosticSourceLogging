using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    public static class Observer
    {
        public static IDisposable Subscribe(IDiagnosticSourceLoggingServiceOptions options, ILoggerFactory loggerFactory)
        {
            return DiagnosticListener
                .AllListeners
                .Subscribe(new DiagnosticListenerObserver(options, loggerFactory));
        }
    }
}