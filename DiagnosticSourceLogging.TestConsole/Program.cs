using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace DiagnosticSourceLogging.TestConsole
{
    class TestService : BackgroundService
    {
        private static readonly DiagnosticListener _Source = new DiagnosticListener("DiagnosticSourceLogging.TestConsole");
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_Source.IsEnabled("ev1"))
                {
                    _Source.Write("ev1", new { arg1 = 1 });
                }
                await Task.Delay(1000);
            }
        }
    }
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
            Console.WriteLine($"IsEnabled: {sourceName}/{eventName}");
            return true;
        }

        public bool ShouldListen(DiagnosticListener listener)
        {
            Console.WriteLine($"ShouldListen: {listener.Name}");
            return listener.Name.StartsWith("DiagnosticSourceLogging.Test");
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(5000));
            await Host.CreateDefaultBuilder()
                .AddDiagnosticSourceLoggingService<MyDiagnosticSourceLoggingServiceOptions>()
                .ConfigureServices((ctx, services) =>
                {
                    services.AddHostedService<TestService>();
                })
                .RunConsoleAsync(cts.Token)
                ;
        }
    }
}
