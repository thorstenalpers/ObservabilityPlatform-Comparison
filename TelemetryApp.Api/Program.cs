using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TelemetryApp.Api.Model;

namespace TelemetryApp.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<TelemetryOptions>(
            builder.Configuration.GetSection(TelemetryOptions.SectionName));

        var telemetryOptions =
            builder.Configuration
                .GetSection(TelemetryOptions.SectionName)
                .Get<TelemetryOptions>()
            ?? throw new InvalidOperationException(
                "OpenObserve configuration missing.");

        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(options =>
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

        builder.Services.AddHealthChecks();
        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddOpenTelemetry()
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

        var app = builder.Build();
        app.MapHealthChecks("/health");
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapGet("/exception", (ILogger<Program> logger) =>
        {
            logger.LogError("Exception endpoint called");
            throw new InvalidOperationException("An exception requested");
        });

        app.MapGet("/weatherforecast", (ILogger<Program> logger) =>
        {
            logger.LogInformation("Weather forecast requested");
            return Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = "Mild"
                });
        });

        app.Run();
    }
}