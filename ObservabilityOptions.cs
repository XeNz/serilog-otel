namespace SerilogTryOut;

public class ObservabilityOptions
{
    public string ServiceName { get; set; } = default!;
    public string CollectorUrl { get; set; } = "http://localhost:4317";

    public bool EnableTracing { get; set; } = false;
    public bool EnableMetrics { get; set; } = false;
    public bool EnablePIIFiltering { get; set; } = false;

    public Uri CollectorUri => new(CollectorUrl);

    public string OtlpLogsCollectorUrl => $"{CollectorUrl}/v1/logs";
}