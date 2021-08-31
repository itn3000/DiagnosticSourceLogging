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
        static readonly DiagnosticSource _DS = new DiagnosticListener($"DiagnosticSourceLogging.DiagnosticSourceLoggingService_{typeof(T).Name}");
        public DiagnosticSourceLoggingService(ILoggerFactory loggerFactory, T options)
        {
            _Options = options;
            _LoggerFactory = loggerFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var subscription = Prepare();
            if(_DS.IsEnabled("Start"))
            {
                _DS.Write("Start", null);
            }
            var sw = new System.Diagnostics.Stopwatch();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(10 * 1000, stoppingToken).ConfigureAwait(false);
                }
                catch(OperationCanceledException)
                {
                    break;
                }
                catch(Exception e)
                {
                    if(_DS.IsEnabled("Error"))
                    {
                        _DS.Write("Error", e);
                    }
                }
            }
            sw.Stop();
            if(_DS.IsEnabled("Stop"))
            {
                _DS.Write("Stop", sw.Elapsed);
            }
        }
        IDisposable Prepare()
        {
            return DiagnosticListener
                .AllListeners
                .Subscribe(new DiagnosticListenerObserver(_Options, _LoggerFactory));
        }
    }
}