using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using TelemetryApp.Api.Model;

namespace TelemetryApp.Api.Extensions
{
    public static class TelemetryServiceCollectionExtensions
    {
        public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            var telemetryOptions = configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>()
                ?? throw new InvalidOperationException("Telemetry configuration missing.");

            if (!telemetryOptions.Enabled)
            {
                return services;
            }

            var serviceVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";

            var telemetryBuilder = services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                {
                    resource
                        .AddService(serviceName: telemetryOptions.ServiceName, serviceVersion: serviceVersion)
                        .AddAttributes(
                        [
                            new KeyValuePair<string, object>("deployment.environment", telemetryOptions.EnvironmentName)
                        ]);
                });

            if (telemetryOptions.EnableTracing)
            {
                telemetryBuilder.WithTracing(tracing =>
                {
                    if (telemetryOptions.EnableAspNetCoreInstrumentation)
                    {
                        // Erfasst eingehende ASP.NET Core HTTP-Anfragen als OpenTelemetry-Traces.
                        tracing.AddAspNetCoreInstrumentation(opt =>
                        {
                            // Exceptions inkl. Stacktrace im Span speichern (hoher Speicherverbrauch)
                            opt.RecordException = telemetryOptions.RecordExceptions;

                            if (telemetryOptions.ExcludeHealthChecks)
                            {
                                opt.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                            }
                        });
                    }

                    if (telemetryOptions.EnableHttpClientInstrumentation)
                    {
                        // Erfasst ausgehende HTTP-Aufrufe über HttpClient als OpenTelemetry-Traces.
                        tracing.AddHttpClientInstrumentation(opt =>
                        {
                            opt.RecordException = telemetryOptions.RecordExceptions;
                        });
                    }

                    if (telemetryOptions.EnableSqlClientInstrumentation)
                    {
                        // Erfasst SQL-Datenbankoperationen als OpenTelemetry-Traces.
                        tracing.AddSqlClientInstrumentation(opt =>
                        {
                            opt.RecordException = telemetryOptions.RecordExceptions;
                        });
                    }
                    // Traces per OTLP an das Telemetrie-Backend senden
                    tracing.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{telemetryOptions.Endpoint}/v1/traces");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        o.Headers = telemetryOptions.Headers;
                    });
                });
            }
            if (telemetryOptions.EnableMetrics)
            {
                telemetryBuilder.WithMetrics(metrics =>
                {
                    if (telemetryOptions.EnableAspNetCoreInstrumentation)
                    {
                        metrics.AddAspNetCoreInstrumentation();
                    }

                    if (telemetryOptions.EnableHttpClientInstrumentation)
                    {
                        metrics.AddHttpClientInstrumentation();
                    }

                    if (telemetryOptions.EnableRuntimeInstrumentation)
                    {
                        metrics.AddRuntimeInstrumentation();
                    }
                    metrics.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{telemetryOptions.Endpoint}/v1/metrics");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        o.Headers = telemetryOptions.Headers;
                    });
                });
            }

            if (telemetryOptions.EnableLogging)
            {
                services.AddLogging(logging =>
                {
                    // Logging:LogLevel:Default steuert die Menge der exportierten Logs.
                    //logging.SetMinimumLevel(LogLevel.Information);
                    logging.AddOpenTelemetry(options =>
                    {
                        options.IncludeScopes = true;               // fügt ILogger-Scope-Daten hinzu (z.B. Request- oder Context-IDs aus BeginScope)
                        options.IncludeFormattedMessage = true;     // besser lesbare logs

                        options.AddOtlpExporter(o =>
                        {
                            o.Endpoint = new Uri($"{telemetryOptions.Endpoint}/v1/logs");
                            o.Protocol = OtlpExportProtocol.HttpProtobuf;
                            o.Headers = telemetryOptions.Headers;
                        });
                    });
                });
            }

            return services;
        }
    }
}
