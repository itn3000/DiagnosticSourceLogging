using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    using LoggerMessageFunction = Action<ILogger, string, Exception>;
    public sealed class EventObserverErrorArg
    {
        public const string Name = "Error";
        public string SourceName { get; }
        public Exception Error { get; }
        internal EventObserverErrorArg(string sourceName, Exception error)
        {
            SourceName = sourceName;
            Error = error;
        }
        public override string ToString()
        {
            return $"{SourceName}: {Error}";
        }
    }
    public sealed class EventObserverProcessErrorArg
    {
        public const string Name = "ProcessError";
        public string SourceName { get; }
        public string EventName { get; }
        public Exception Error { get; }
        internal EventObserverProcessErrorArg(string sourceName, string eventName, Exception error)
        {
            SourceName = sourceName;
            EventName = eventName;
            Error = error;
        }
        public override string ToString()
        {
            return $"{SourceName}/{EventName}: {Error}";
        }
    }
    public sealed class EventObserverCompletedArg
    {
        public const string Name = "Completed";
        public string SourceName { get; }
        internal EventObserverCompletedArg(string sourceName)
        {
            SourceName = sourceName;
        }
        public override string ToString()
        {
            return SourceName;
        }
    }
    internal sealed class EventObserver : IObserver<KeyValuePair<string, object>>
    {
        private static readonly DiagnosticListener _InternalSource = new DiagnosticListener(Constants.EventObserverSourceName);
        ILogger _Logger;
        IDiagnosticSourceLoggingServiceOptions _Options;
        string _SourceName;
        public EventObserver(string sourceName, ILogger logger, IDiagnosticSourceLoggingServiceOptions options)
        {
            _SourceName = sourceName;
            _Logger = logger;
            _Options = options;
        }
        public void OnCompleted()
        {
            if (_InternalSource.IsEnabled(EventObserverCompletedArg.Name))
            {
                _InternalSource.Write(EventObserverCompletedArg.Name, new EventObserverCompletedArg(_SourceName));
            }
        }

        public void OnError(Exception error)
        {
            if (_InternalSource.IsEnabled(EventObserverErrorArg.Name))
            {
                _InternalSource.Write(EventObserverErrorArg.Name, new EventObserverErrorArg(_SourceName, error));
            }
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            var action = _Options.GetEventProcessor(_SourceName, value.Key);
            try
            {
                action(_Logger, value.Key, value.Value);
            }
            catch (Exception e)
            {
                if (_InternalSource.IsEnabled(EventObserverProcessErrorArg.Name))
                {
                    _InternalSource.Write(EventObserverProcessErrorArg.Name, new EventObserverProcessErrorArg(_SourceName, value.Key, e));
                }
            }
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
            foreach (var item in _Subscriptions.Keys)
            {
                if (_Subscriptions.TryRemove(item, out var value))
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