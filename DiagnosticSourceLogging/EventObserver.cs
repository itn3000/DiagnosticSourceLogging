using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DiagnosticSourceLogging
{
    /// <summary>DiagnosticSource event argument triggered by unexpected error in observing event</summary>
    /// <remarks>DiagnosticSource name is 'DiagnosticSourceLogging.EventObserver'</remarks>
    public sealed class EventObserverErrorArg
    {
        public const string Name = "Error";
        /// <summary>observed DiagnosticSource name</summary>
        public string SourceName { get; }
        /// <summary>error data</summary>
        public Exception Error { get; }
        internal EventObserverErrorArg(string sourceName, Exception error)
        {
            SourceName = sourceName;
            Error = error;
        }
        public override string ToString()
        {
            return $"Event Observation Error: {SourceName}: {Error}";
        }
    }
    /// <summary>DiagnosticSource event argument triggered by error in processing event</summary>
    /// <remarks>DiagnosticSource name is 'DiagnosticSourceLogging.EventObserver'</remarks>
    public sealed class EventObserverProcessErrorArg
    {
        public const string Name = "ProcessError";
        /// <summary>observed DiagnosticSource name</summary>
        public string SourceName { get; }
        /// <summary>error event name</summary>
        public string EventName { get; }
        /// <summary>error data</summary>
        public Exception Error { get; }
        internal EventObserverProcessErrorArg(string sourceName, string eventName, Exception error)
        {
            SourceName = sourceName;
            EventName = eventName;
            Error = error;
        }
        public override string ToString()
        {
            return $"Event Observation Process Error: {SourceName}/{EventName}: {Error}";
        }
    }
    /// <summary>DiagnosticSource event argument triggered by completing event observing</summary>
    /// <remarks>DiagnosticSource name is 'DiagnosticSourceLogging.EventObserver'</remarks>
    public sealed class EventObserverCompletedArg
    {
        public const string Name = "Completed";
        /// <summary>DiagnosticSource name</summary>
        public string SourceName { get; }
        internal EventObserverCompletedArg(string sourceName)
        {
            SourceName = sourceName;
        }
        public override string ToString()
        {
            return $"Event Observation Completed: {SourceName}";
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
}