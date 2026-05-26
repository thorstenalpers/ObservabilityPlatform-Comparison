using Microsoft.EntityFrameworkCore;
using TelemetryApp.Api.Model;

namespace TelemetryApp.Api;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<WeatherForecast> WeatherForecasts => Set<WeatherForecast>();
}
