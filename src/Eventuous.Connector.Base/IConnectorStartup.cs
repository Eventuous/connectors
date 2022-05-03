using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Eventuous.Connector.Base; 

public interface IConnectorStartup {
    ConnectorApp BuildConnectorApp(
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricExporters
    );
}
