using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Windows;
using TelemetryApp.Api.Model;

namespace TelemetryApp.Wpf;

public partial class App : Application
{
    public static IConfiguration Configuration { get; private set; } = null!;
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
             .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json",
                 optional: true,
                 reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();

        services.Configure<TelemetryOptions>(Configuration.GetSection(TelemetryOptions.SectionName));

        var telemetryOptions = Configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>()!;

        services.AddSingleton<MainWindow>();
        services.AddLogging(logging =>
        {
            logging.ClearProviders();

            //logging.SetMinimumLevel(LogLevel.Trace);  // steuern der generierten OTEL-logs

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

        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                // Gemeinsame Metadaten für Logs, Traces und Metriken festlegen
                resource
                    .AddService(serviceName: "TelemetryApp.Wpf", serviceVersion: "1.0.0.1")
                    .AddAttributes([new KeyValuePair<string, object>("environment", "Client-PC")]);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(options =>  // HTTP-Requests der ASP.NET Core Anwendung automatisch tracen
                    {
                        options.RecordException = true;       // Exceptions inkl. Stacktrace im Span speichern (hoher Speicherverbrauch)
                    })
                    .AddSqlClientInstrumentation(options =>   // SQL-Datenbankaufrufe automatisch tracen
                    {
                        options.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(options =>  // Ausgehende HTTP-Aufrufe automatisch tracen
                    {
                        options.RecordException = true;
                    })
                    .AddOtlpExporter(o =>                     // Traces per OTLP an das Telemetrie-Backend senden
                    {
                        o.Endpoint = new Uri($"{telemetryOptions.Endpoint}/v1/traces");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        o.Headers = telemetryOptions.Headers;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri($"{telemetryOptions.Endpoint}/v1/metrics");
                        o.Protocol = OtlpExportProtocol.HttpProtobuf;
                        o.Headers = telemetryOptions.Headers;
                    });
            });

        Services = services.BuildServiceProvider();
        var logger = Services.GetRequiredService<ILogger<App>>();

        logger.LogInformation("WPF application starting");

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var logger = Services.GetRequiredService<ILogger<App>>();

        logger.LogInformation("WPF application stopping");

        base.OnExit(e);
    }
}