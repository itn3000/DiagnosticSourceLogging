using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    internal class EventObserver : IObserver<KeyValuePair<string, object>>
    {
        private static readonly DiagnosticSource _InternalSource = new DiagnosticListener(nameof(DiagnosticSourceLogging.EventObserver));
        Func<FormatterArg, Exception, string> Formatter;
        ILogger _Logger;
        Func<string, string, LogLevel> _LogLevelGetter;
        string _SourceName;
        public EventObserver(string sourceName, Func<FormatterArg, Exception, string> formatter, Func<string, string, LogLevel> logLevelGetter, ILogger logger)
        {
            _SourceName = sourceName;
            Formatter = formatter;
            _Logger = logger;
            _LogLevelGetter = logLevelGetter;
        }
        public void OnCompleted()
        {
            _InternalSource.Write("Completed", new { SourceName = _SourceName });
        }

        public void OnError(Exception error)
        {
            _InternalSource.Write("Error", new { SourceName = _SourceName, Error = error });
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            _Logger.Log(_LogLevelGetter(_SourceName, value.Key), (EventId)1, new FormatterArg(_SourceName, value.Key, value.Value), null, Formatter);
        }
    }
    internal class DiagnosticSourceListenerObserver : IObserver<DiagnosticListener>
    {
        public DiagnosticSourceListenerObserver(IDiagnosticSourceLoggingServiceOptions options, ILoggerFactory loggerFactory)
        {
            _Options = options;
            _LoggerFactory = loggerFactory;
        }
        private static readonly DiagnosticListener _InternalSource = new DiagnosticListener(nameof(DiagnosticSourceLogging.DiagnosticSourceListenerObserver));
        ConcurrentDictionary<string, IDisposable> _Subscriptions = new ConcurrentDictionary<string, IDisposable>();
        IDiagnosticSourceLoggingServiceOptions _Options;
        ILoggerFactory _LoggerFactory;
        public void OnCompleted()
        {
            if (_InternalSource.IsEnabled("Completed"))
            {
                _InternalSource.Write("Completed", new { Name = _InternalSource.Name });
            }
        }

        public void OnError(Exception error)
        {
            if (_InternalSource.IsEnabled("Error"))
            {
                _InternalSource.Write("Error", new { Name = _InternalSource.Name, Error = error });
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if (_Options.ShouldListen(value) && _Subscriptions.TryAdd(value.Name, null))
            {
                _Subscriptions[value.Name] = value
                    .Subscribe(new EventObserver(value.Name, _Options.Formatter, _Options.LogLevelGetter, _LoggerFactory.CreateLogger(value.Name)),
                        _Options.IsEnabled);
            }
        }
    }
}