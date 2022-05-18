using Eventuous.Connector;
using Eventuous.Connector.Base.Diag;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

TypeMap.RegisterKnownEventTypes();

var tracingExporters = new ExporterMappings<TracerProviderBuilder>()
    .Add("otlp", b => b.AddOtlpExporter())
    .Add("zipkin", b => b.AddZipkinExporter())
    .Add("jaeger", b => b.AddJaegerExporter());

var metricsExporters = new ExporterMappings<MeterProviderBuilder>()
    .Add("prometheus", b => b.AddPrometheusExporter())
    .Add("otlp", b => b.AddOtlpExporter());

using var app = new StartupBuilder()
    .Configure("config.yaml", args)
    .BuildApplication(tracingExporters, metricsExporters);

return await app.Run();
