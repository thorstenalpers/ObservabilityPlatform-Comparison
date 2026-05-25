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
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;

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
                resource.AddService(
                    telemetryOptions.ServiceName,
                    telemetryOptions.ServiceVersion);
            })
              .WithTracing(tracing =>
              {
                  tracing
                      .AddAspNetCoreInstrumentation()
                      .AddHttpClientInstrumentation()
                      .AddOtlpExporter(o =>
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
        var logger =
            Services.GetRequiredService<ILogger<App>>();

        logger.LogInformation("WPF application stopping");

        base.OnExit(e);
    }
}