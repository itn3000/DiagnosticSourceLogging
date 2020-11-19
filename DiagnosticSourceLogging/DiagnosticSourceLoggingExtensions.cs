using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DiagnosticSourceLogging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DiagnosticSourceLoggingExtensions
    {
        public static IHostBuilder AddDiagnosticSourceLoggingService<T>(this IHostBuilder hostbuilder,
            Func<IServiceProvider, T> optionsFactory = null) where T : class, IDiagnosticSourceLoggingServiceOptions
        {
            return hostbuilder.ConfigureServices(builder =>
            {
                if (optionsFactory != null)
                {
                    builder.AddTransient<T>(optionsFactory);
                }
                else
                {
                    builder.AddTransient<T>();
                }
                builder.AddHostedService<DiagnosticSourceLoggingService<T>>();
            });
        }
    }
}