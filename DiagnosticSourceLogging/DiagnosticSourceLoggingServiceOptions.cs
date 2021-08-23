using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace DiagnosticSourceLogging
{
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
        /// <summary>get or create callback for DiagnosticSource events</summary>
        /// <param name="sourceName">equals to DiagnosticSource.Name</param>
        /// <param name="eventName">equals to DiagnosticSource event name</param>
        /// <remarks>this is called when every DiagnosticSource events, so you should cache delegate for performance.</remarks>
        /// <returns>callback for DiagnosticSource event</returns>
        Action<ILogger, string, object> GetEventProcessor(string sourceName, string eventName);
    }
}