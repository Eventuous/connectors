using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.Connector.EsdbElastic.Defaults;
using Eventuous.Connector.Base;
using Eventuous.ElasticSearch.Store;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Eventuous.Connector.EsdbElastic;

[UsedImplicitly]
public class ConnectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp
            .Create<EsdbConfig, ElasticConfig>()
            .RegisterDependencies(
                (services, config) => Startup.Register<PersistedEvent>(
                    services,
                    config,
                    def => new DefaultElasticSerializer(def)
                )
            )
            .RegisterConnector(
                (cfg, config) => {
                    var indexName = Ensure.NotEmptyString(config.Target.DataStream?.IndexName);

                    return Startup.ConfigureConnector(
                        cfg,
                        config,
                        _ => new DefaultElasticTransform(indexName)
                    );
                }
            )
            .AddOpenTelemetry(
                (cfg, enrich) =>
                    cfg
                        .AddGrpcClientInstrumentation(options => options.Enrich = enrich)
                        .AddElasticsearchClientInstrumentation(options => options.Enrich = enrich),
                tracingExporters: tracingExporters,
                metricsExporters: metricsExporters
            );

        return builder.Build();
    }
}
