using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    using LoggerMessageFunction = Action<ILogger, string, Exception>;
    /// <summary>DiagnosticSource event argument triggered by starting DiagnosticSource observation</summary>
    /// <remarks>Source name is 'DiagnosticSourceLogging.DiagnosticListenerObserver'</remarks>
    public sealed class ObserverStartingArg
    {
        public const string Name = "Starting";
        public string SourceName { get; }
        public string TypeName { get; }
        internal ObserverStartingArg(string typeName, string sourceName)
        {
            SourceName = sourceName;
            TypeName = typeName;
        }
        public override string ToString()
        {
            return $"Observation Starting: {TypeName}/{SourceName}";
        }
    }
    /// <summary>DiagnosticSource event argument triggered by completing DiagnosticSource observation</summary>
    /// <remarks>Source name is 'DiagnosticSourceLogging.DiagnosticListenerObserver'</remarks>
    public sealed class ObserverCompletedArg
    {
        public const string Name = "Completed";
        /// <summary>DiagnosticSource name</summary>
        public string TypeName { get; }
        internal ObserverCompletedArg(string typeName)
        {
            TypeName = typeName;
        }
        public override string ToString()
        {
            return $"Observation Completed: {TypeName}";
        }
    }
    /// <summary>DiagnosticSource event argument triggered by unexpected error in DiagnosticSource observation</summary>
    /// <remarks>Source name is 'DiagnosticSourceLogging.DiagnosticListenerObserver'</remarks>
    public sealed class ObserverErrorArg
    {
        public const string Name = "Error";
        /// <summary>error data</summary>
        public Exception Error { get; }
        public string TypeName { get; }
        internal ObserverErrorArg(string typeName, Exception error)
        {
            TypeName = typeName;
            Error = error;
        }
        public override string ToString()
        {
            return $"Observation Error: {TypeName}: {Error}";
        }
    }
    internal sealed class DiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        string _OptionName;
        public DiagnosticListenerObserver(IDiagnosticSourceLoggingServiceOptions options, ILoggerFactory loggerFactory)
        {
            _Options = options;
            _LoggerFactory = loggerFactory;
            _OptionName = options.GetType().Name;
        }
        private static readonly DiagnosticListener _InternalSource = new DiagnosticListener($"{nameof(DiagnosticSourceLogging)}.{nameof(DiagnosticListenerObserver)}");
        ConcurrentDictionary<string, IDisposable> _Subscriptions = new ConcurrentDictionary<string, IDisposable>();
        IDiagnosticSourceLoggingServiceOptions _Options;
        ILoggerFactory _LoggerFactory;
        public void OnCompleted()
        {
            if (_InternalSource.IsEnabled(ObserverCompletedArg.Name))
            {
                _InternalSource.Write(ObserverCompletedArg.Name, new ObserverCompletedArg(_OptionName));
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
            if (_InternalSource.IsEnabled(ObserverErrorArg.Name))
            {
                _InternalSource.Write(ObserverErrorArg.Name, new ObserverErrorArg(_OptionName, error));
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if (_Options.ShouldListen(value) && _Subscriptions.TryAdd(value.Name, null))
            {
                if (_InternalSource.IsEnabled(ObserverStartingArg.Name))
                {
                    _InternalSource.Write(ObserverStartingArg.Name, new ObserverStartingArg(_OptionName, value.Name));
                }
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