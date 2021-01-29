using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

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
        public EventId GetEventId(string sourceName, string eventName)
        {
            return new EventId(1, $"{sourceName}.{eventName}");
        }

        public string GetFormattedString(string sourceName, string eventName, object arg)
        {
            dynamic x = arg;
            return $"{sourceName}/{eventName}: {x.arg1}";
        }

        public LogLevel GetLogLevel(string sourceName, string eventName)
        {
            return LogLevel.Information;
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
