using connector_esdb_elastic;
using Eventuous.Connector.EsdbElastic;
using Eventuous.Connectors.Base;
using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.ElasticSearch.Store;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ElasticSerializer = connector_esdb_elastic.ElasticSerializer;

TypeMap.RegisterKnownEventTypes();

var tracingExporters = new ExporterMappings<TracerProviderBuilder>()
    .Add("otlp", b => b.AddOtlpExporter())
    .Add("zipkin", b => b.AddZipkinExporter())
    .Add("jaeger", b => b.AddJaegerExporter());

var metricsExporters = new ExporterMappings<MeterProviderBuilder>()
    .Add("prometheus", b => b.AddPrometheusExporter())
    .Add("otlp", b => b.AddOtlpExporter());

var builder = ConnectorApp
    .Create<EsdbConfig, ElasticConfig>()
    .RegisterDependencies(
        (services, config) => Startup.Register<PersistedEvent>(
            services,
            config,
            def => new ElasticSerializer(def)
        )
    )
    .RegisterConnector(
        (cfg, config) => {
            var indexName = Ensure.NotEmptyString(config.Target.DataStream?.IndexName);
            return Startup.ConfigureConnector(cfg, config, _ => new EventTransform(indexName));
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

var app = builder.Build();
app.Host.UseOpenTelemetryPrometheusScrapingEndpoint();
await app.Run();
