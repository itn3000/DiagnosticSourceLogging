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
        public Func<DiagnosticListener, bool> ShouldListen => x => x.Name.StartsWith("DiagnosticSourceLogging.Test");

        public Func<string, string, object, object, bool> IsEnabled => (sourceName, eventName, arg1, arg2) => true;

        public Func<FormatterArg, Exception, string> Formatter => (arg, e) => $"{arg.SourceName}/{arg.EventName}: {arg.Arg}";

        public Func<string, string, LogLevel> LogLevelGetter => (sourceName, eventName) => LogLevel.Information;
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
