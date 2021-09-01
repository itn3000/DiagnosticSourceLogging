using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    public static class Observable
    {
        class DelegateDiagnosticSourceLoggingOptions : IDiagnosticSourceLoggingServiceOptions
        {
            Func<string, string, Action<ILogger, string, object>> _EventProcessorFactory;
            Func<string, string, object, object, bool> _IsEnabled;
            Func<DiagnosticListener, bool> _ShouldListen;
            public DelegateDiagnosticSourceLoggingOptions(
                Func<string, string, Action<ILogger, string, object>> eventProcessorFactory,
                Func<DiagnosticListener, bool> shouldListen,
                Func<string, string, object, object, bool> isEnabled
                )
            {
                _EventProcessorFactory = eventProcessorFactory;
                _IsEnabled = isEnabled;
                _ShouldListen = shouldListen;
            }
            
            public Action<ILogger, string, object> GetEventProcessor(string sourceName, string eventName)
            {
                return _EventProcessorFactory(sourceName, eventName);
            }

            public bool IsEnabled(string sourceName, string eventName, object arg1, object arg2)
            {
                return _IsEnabled(sourceName, eventName, arg1, arg2);
            }

            public bool ShouldListen(DiagnosticListener listener)
            {
                return _ShouldListen(listener);
            }
        }
        public static IDisposable Subscribe(ILoggerFactory loggerFactory, IDiagnosticSourceLoggingServiceOptions options)
        {
            return DiagnosticListener
                .AllListeners
                .Subscribe(new DiagnosticListenerObserver(options, loggerFactory));
        }
        public static IDisposable Subscribe(
            ILoggerFactory loggerFactory,
            Func<DiagnosticListener, bool> shouldListen,
            Func<string, string, Action<ILogger, string, object>> eventProcessorFactory,
            Func<string, string, object, object, bool> isEnabled = null
            )
        {
            return Subscribe(loggerFactory, new DelegateDiagnosticSourceLoggingOptions(eventProcessorFactory, shouldListen,
                isEnabled != null ? isEnabled : (src, ev, arg1, arg2) => true));
        }
    }
}