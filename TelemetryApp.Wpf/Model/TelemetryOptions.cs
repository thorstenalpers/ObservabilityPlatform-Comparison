namespace TelemetryApp.Api.Model;

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public string Endpoint { get; set; } = string.Empty;
    public string Headers { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceVersion { get; set; } = string.Empty;
}
