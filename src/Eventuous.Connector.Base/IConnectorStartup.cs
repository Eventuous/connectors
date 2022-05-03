using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Diag;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Eventuous.Connector.Base; 

public interface IConnectorStartup {
    ConnectorApp BuildConnectorApp(
        string configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricExporters
    );
}
