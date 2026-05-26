using Microsoft.EntityFrameworkCore;
using TelemetryApp.Api.Extensions;
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
        builder.Services.AddTelemetry(builder.Configuration, builder.Environment);

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