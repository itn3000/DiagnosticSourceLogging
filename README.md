# DiagnosticSource output helper to ILogger

DiagnosticSourceLogging is helper package for outputting DiagnosticSource to ILogger.
This uses [generic host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host) for listening thread.

# Usage

## Package installation

add following nuget packages to your project

* DiagnosticSourceLogging
* Microsoft.Extensions.Hosting
* [any package of ILogger destination](https://www.nuget.org/packages?q=Microsoft.Extensions.Logging)


## Implement IDiagnosticSourceLoggingServiceOptions

After your nuget package setting,implement `DiagnosticSourceLogging.IDiagnosticSourceLoggingServiceOptions` for listener setting.
Here is the IDiagnosticSourceLoggingServiceOptions's members

* `bool ShouldListen(DiagnosticListener listener)`
    * It should return whether the DiagnosticListener should be watched
    * If it returns `true`, Logging will be started
    * It may be called multiple times in same listener instance
    * It is not called frequently, almost once per one DiagnosticListener
* `bool IsEnabled(string sourceName, string eventName)`
    * It should return whether events should be processed
    * If it returns `true`, GetEventProcessor will be called and invoking returned callback
    * It may be call many times, so heavy process should not run in it.
* `Action<ILogger, string, object> GetEventProcessor(string sourceName, string eventName)`
    * It should return callback for DiagnosticSource events
    * This will be called every events occured, so return value should be cached for almost cases

Here is the example implementation:

```csharp
class MyDiagnosticSourceLoggingServiceOptions : IDiagnosticSourceLoggingServiceOptions
{
    ConcurrentDictionary<(string sourceName, string eventName), Action<ILogger, string, object>> Processors = new();
    public Action<ILogger, string, object> GetEventProcessor(string sourceName, string eventName)
    {
        return Processors.GetOrAdd((sourceName, eventName), n =>
        {
            var f = LoggerMessage.Define<string, object>(LogLevel.Information, new EventId(1, $"{n.sourceName}"), "{0}: {1}");
            return (ILogger logger, string msg, object arg) => f(logger, msg, arg, null);
        });
    }

    public bool IsEnabled(string sourceName, string eventName, object arg1, object arg2)
    {
        return true;
    }

    public bool ShouldListen(DiagnosticListener listener)
    {
        return listener.Name.StartsWith("DiagnosticSourceLogging.Test");
    }
}
```

## Adding task

DiagnosticSourceLogging needs background task which holds subscription for DiagnosticListener.
Background task can be added by `AddDiagnosticSourceLoggingService<T>(this IHostBuilder builder, Func<T> optionsFactory = null) where T: IDiagnosticSourceLoggingServiceOptions`.

Example:

```csharp
// Generally, CreateDefaultBuilder configures ConsoleLogger.
await Host.CreateDefaultBuilder()
    // configuring logging...
    .AddDiagnosticSourceLoggingService<MyDiagnosticSourceLoggingServiceOptions>()
    // add your tasks...
    .RunConsoleAsync()
    ;
```