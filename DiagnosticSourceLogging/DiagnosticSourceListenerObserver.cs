using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    using LoggerMessageFunction = Action<ILogger, string, Exception>;
    internal sealed class EventObserver : IObserver<KeyValuePair<string, object>>
    {
        private readonly ConcurrentDictionary<(string sourceName, string eventName), LoggerMessageFunction> LoggerMessages = new ConcurrentDictionary<(string sourceName, string eventName), LoggerMessageFunction>();
        private static readonly DiagnosticSource _InternalSource = new DiagnosticListener($"{nameof(DiagnosticSourceLogging)}.{nameof(EventObserver)}");
        Func<FormatterArg, Exception, string> Formatter;
        ILogger _Logger;
        IDiagnosticSourceLoggingServiceOptions _Options;
        Func<string, string, LogLevel> _;
        Func<string, string, EventId> _EventIdGetter;
        string _SourceName;
        public EventObserver(string sourceName, ILogger logger, IDiagnosticSourceLoggingServiceOptions options)
        {
            _SourceName = sourceName;
            _Logger = logger;
            _Options = options;
        }
        public void OnCompleted()
        {
            if(_InternalSource.IsEnabled("Completed"))
            {
                _InternalSource.Write("Completed", new { SourceName = _SourceName });
            }
        }

        public void OnError(Exception error)
        {
            if(_InternalSource.IsEnabled("Error"))
            {
                _InternalSource.Write("Error", new { SourceName = _SourceName, Error = error });
            }
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            _Logger.Log(_Options.GetLogLevel(_SourceName, value.Key),
                _Options.GetEventId(_SourceName, value.Key)
                , (source: _SourceName, value: value), null, (state, e) => _Options.Formatter(state.source, state.value, e));
            if(!LoggerMessages.TryGetValue((_SourceName, value.Key), out var action))
            {
                action = LoggerMessage.Define<string>(_Options.GetLogLevel(_SourceName, value.Key),
                    _Options.GetEventId(_SourceName, value.Key),
                    "{0}");
                LoggerMessages.TryAdd((_SourceName, value.Key), action);
            }
            action(_Logger, _Options.GetFormattedString(_SourceName, value.Key, value.Value), null);
        }
    }
    internal sealed class DiagnosticSourceListenerObserver : IObserver<DiagnosticListener>
    {
        public DiagnosticSourceListenerObserver(IDiagnosticSourceLoggingServiceOptions options, ILoggerFactory loggerFactory)
        {
            _Options = options;
            _LoggerFactory = loggerFactory;
        }
        private static readonly DiagnosticListener _InternalSource = new DiagnosticListener($"{nameof(DiagnosticSourceLogging)}.{nameof(DiagnosticSourceListenerObserver)}");
        ConcurrentDictionary<string, IDisposable> _Subscriptions = new ConcurrentDictionary<string, IDisposable>();
        IDiagnosticSourceLoggingServiceOptions _Options;
        ILoggerFactory _LoggerFactory;
        public void OnCompleted()
        {
            if (_InternalSource.IsEnabled("Completed"))
            {
                _InternalSource.Write("Completed", new { Name = _InternalSource.Name });
            }
            foreach(var item in _Subscriptions.Keys)
            {
                if(_Subscriptions.TryRemove(item, out var value))
                {
                    value?.Dispose();
                }
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
                string sourceName = value.Name;
                _Subscriptions[sourceName] = value
                    .Subscribe(new EventObserver(sourceName,
                        _LoggerFactory.CreateLogger(sourceName),
                        _Options),
                        (evname, arg1, arg2) => _Options.IsEnabled(sourceName, evname, arg1, arg2));
            }
        }
    }
}