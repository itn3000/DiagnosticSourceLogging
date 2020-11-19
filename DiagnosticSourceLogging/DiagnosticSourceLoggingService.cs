using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace DiagnosticSourceLogging
{
    public sealed class DiagnosticSourceLoggingService<T> : BackgroundService where T : class, IDiagnosticSourceLoggingServiceOptions
    {
        T _Options;
        ILoggerFactory _LoggerFactory;
        public DiagnosticSourceLoggingService(ILoggerFactory loggerFactory, T options)
        {
            _Options = options;
            _LoggerFactory = loggerFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var subscription = Prepare();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10 * 1000, stoppingToken).ConfigureAwait(false);
                }
                catch
                {

                }

            }
        }
        IDisposable Prepare()
        {
            return DiagnosticListener
                .AllListeners
                .Subscribe(new DiagnosticSourceListenerObserver(_Options, _LoggerFactory));
        }
    }
}