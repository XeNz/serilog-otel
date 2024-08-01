using Destructurama;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Sinks.OpenTelemetry;
using ExportProcessorType = OpenTelemetry.ExportProcessorType;

namespace SerilogTryOut;

public static class ObservabilityRegistration
{
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        ObservabilityOptions observabilityOptions = new();

        configuration
            .GetRequiredSection(nameof(ObservabilityOptions))
            .Bind(observabilityOptions);

        builder.AddSerilog(observabilityOptions);
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(observabilityOptions.ServiceName))
            .AddMetrics(observabilityOptions)
            .AddTracing(observabilityOptions);

        return builder;
    }

    private static IOpenTelemetryBuilder AddTracing(this IOpenTelemetryBuilder builder, ObservabilityOptions observabilityOptions)
    {
        if (!observabilityOptions.EnableTracing) return builder;

        builder.WithTracing(tracing =>
        {
            tracing
                .SetErrorStatusOnException()
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                });

            tracing
                .AddOtlpExporter(exporterOptions =>
                {
                    exporterOptions.Endpoint = observabilityOptions.CollectorUri;
                    exporterOptions.ExportProcessorType = ExportProcessorType.Batch;
                    exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
        });

        return builder;
    }

    private static IOpenTelemetryBuilder AddMetrics(this IOpenTelemetryBuilder builder, ObservabilityOptions observabilityOptions)
    {
        builder.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation();

            metrics
                .AddOtlpExporter(exporterOptions =>
                {
                    exporterOptions.Endpoint = observabilityOptions.CollectorUri;
                    exporterOptions.ExportProcessorType = ExportProcessorType.Batch;
                    exporterOptions.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                });
        });

        return builder;
    }

    private static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder, ObservabilityOptions observabilityOptions)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        services.AddSerilog((sp, serilog) =>
        {
            serilog
                .ReadFrom.Configuration(configuration,
                    new ConfigurationReaderOptions
                    {
                        SectionName = $"{nameof(ObservabilityOptions)}:{nameof(Serilog)}"
                    })
                .ReadFrom.Services(sp)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", observabilityOptions.ServiceName)
                .WriteTo.Console();

            if (observabilityOptions.EnablePIIFiltering)
            {
                serilog.Destructure.UsingAttributes();
            }

            serilog
                .WriteTo.OpenTelemetry(c =>
                {
                    c.Endpoint = observabilityOptions.CollectorUrl;
                    c.Protocol = OtlpProtocol.Grpc;
                    c.IncludedData = IncludedData.TraceIdField | IncludedData.SpanIdField | IncludedData.SourceContextAttribute;
                    c.ResourceAttributes = new Dictionary<string, object>
                    {
                        { "service.name", observabilityOptions.ServiceName },
                        { "index", 10 },
                        { "flag", true },
                        { "value", 3.14 }
                    };
                });
        });

        return builder;
    }
}