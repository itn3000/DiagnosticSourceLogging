using System;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Threading;

namespace DiagnosticSourceLogging.Test
{
    class MyDiagnosticSourceLoggingServiceOptions : IDiagnosticSourceLoggingServiceOptions
    {
        public string Formatter(FormatterArg arg, Exception error)
        {
            return $"{arg.SourceName}/{arg.EventName}: {arg.Arg}";
        }

        public EventId GetEventId(string sourceName, string eventName)
        {
            return new EventId(1, $"{sourceName}.{eventName}");
        }

        public string GetFormattedString(string sourceName, string eventName, object arg)
        {
            return $"{sourceName}/{eventName}: {arg}";
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
    public class UnitTest1
    {
        class TestService : BackgroundService
        {
            private static readonly DiagnosticListener _Source = new DiagnosticListener(nameof(DiagnosticSourceLogging.Test));
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while(!stoppingToken.IsCancellationRequested)
                {
                    if(_Source.IsEnabled("ev1"))
                    {
                        _Source.Write("ev1", new { arg1 = 1 });
                    }
                    await Task.Delay(10);
                }
            }
        }
        [Fact]
        public async Task TestHostedService()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await  Host.CreateDefaultBuilder()
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
