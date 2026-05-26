using Microsoft.EntityFrameworkCore;
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

        builder.Services.Configure<TelemetryOptions>(builder.Configuration.GetSection(TelemetryOptions.SectionName));

        var telemetryOptions = builder.Configuration.GetSection(TelemetryOptions.SectionName).Get<TelemetryOptions>()
            ?? throw new InvalidOperationException("Telemetry configuration missing.");

        builder.Logging.ClearProviders();
        builder.Logging.AddOpenTelemetry(options =>
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

        //builder.Logging.SetMinimumLevel(LogLevel.Trace);  // steuern der generierten OTEL-logs

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                // Gemeinsame Metadaten für Logs, Traces und Metriken festlegen
                resource
                    .AddService(serviceName: "TelemetryApp.Api", serviceVersion: "1.0.0.1")
                    .AddAttributes([new KeyValuePair<string, object>("environment", "web-server")]);
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

        builder.Services.AddHealthChecks();
        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        var app = builder.Build();
        app.MapHealthChecks("/health");
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapGet("/exception", (ILogger<Program> logger) =>
            {
                logger.LogDebug("Exception endpoint called");
                logger.LogInformation("Exception endpoint called");
                logger.LogWarning("Exception endpoint called");

                throw new InvalidOperationException("An exception occurred here!!!!!!");
            })
            .WithName("CreateException");

        app.MapGet("/weatherforecast", async (AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogDebug("Get Weatherforecast endpoint called");
                logger.LogInformation("Get Weatherforecast endpoint called");
                logger.LogWarning("Get Weatherforecast endpoint called");

                await Task.Delay(TimeSpan.FromSeconds(10));

                var entities = await db.WeatherForecasts.ToListAsync();
                return Results.Ok(entities);
            })
            .WithName("GetWeatherForecasts");

        app.MapPost("/weatherforecast", async (WeatherForecast forecast, AppDbContext db, ILogger<Program> logger) =>
            {
                logger.LogDebug("Post Weatherforecast endpoint called");
                logger.LogInformation("Post Weatherforecast endpoint called");
                logger.LogWarning("Post Weatherforecast endpoint called");

                db.WeatherForecasts.Add(forecast);
                await db.SaveChangesAsync();
                return Results.Created($"/weatherforecast/{forecast.Id}", forecast);
            })
            .WithName("PostWeatherForecast");

        app.Run();
    }
}