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
        public Func<DiagnosticListener, bool> ShouldListen => x => 
        {
            return x.Name.StartsWith("DiagnosticSourceLogging");
        };

        public Func<string, string, object, object, bool> IsEnabled => (sourceName, eventName, arg1, arg2) => true;

        public Func<FormatterArg, Exception, string> Formatter => (arg, e) => $"{arg.SourceName}/{arg.EventName}: {arg.Arg}";

        public Func<string, string, LogLevel> LogLevelGetter => (sourceName, eventName) => LogLevel.Information;
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
