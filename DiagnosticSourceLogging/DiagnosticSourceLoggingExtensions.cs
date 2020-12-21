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
                    builder.AddSingleton<T>(optionsFactory);
                }
                else
                {
                    builder.AddSingleton<T>();
                }
                builder.AddHostedService<DiagnosticSourceLoggingService<T>>();
            });
        }
        public static IHostBuilder AddDiagnosticSourceLoggingService<T>(this IHostBuilder hostbuilder,
            T options) where T : class, IDiagnosticSourceLoggingServiceOptions
        {
            return hostbuilder.ConfigureServices(builder =>
            {
                builder.AddSingleton(options);
                builder.AddHostedService<DiagnosticSourceLoggingService<T>>();
            });
        }
    }
}