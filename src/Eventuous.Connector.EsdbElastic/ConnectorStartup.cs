using Elasticsearch.Net;
using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.Connector.EsdbElastic.Defaults;
using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.EsdbElastic.Conversions;
using Eventuous.Connector.EsdbElastic.Infrastructure;
using Eventuous.ElasticSearch.Index;
using Eventuous.ElasticSearch.Producers;
using Eventuous.ElasticSearch.Projections;
using Eventuous.ElasticSearch.Store;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Gateway;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Eventuous.Connector.EsdbElastic;

[UsedImplicitly]
public class ConnectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        string configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp
            .Create<EsdbConfig, ElasticConfig>(configFile)
            .RegisterDependencies(
                (services, config) => Register<PersistedEvent>(
                    services,
                    config,
                    def => new DefaultElasticSerializer(def)
                )
            )
            .RegisterConnector(
                (cfg, config) => {
                    var indexName = Ensure.NotEmptyString(config.Target.DataStream?.IndexName);

                    return ConfigureConnector(
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

    static void Register<T>(
        IServiceCollection                                       services,
        ConnectorConfig<EsdbConfig, ElasticConfig>               config,
        Func<IElasticsearchSerializer, IElasticsearchSerializer> getSerializer
    ) where T : class {
        var dataStreamConfig = Ensure.NotNull(config.Target.DataStream);
        services.AddSingleton(dataStreamConfig);

        services
            .AddEventStoreClient(
                Ensure.NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string")
            )
            .AddElasticClient(
                config.Target.ConnectionString,
                config.Target.CloudId,
                config.Target.ApiKey,
                getSerializer
            );
        services.AddStartupJob<IElasticClient, IndexConfig>(SetupIndex.CreateIndexIfNecessary<T>);
    }

    static ConnectorBuilder<
            AllStreamSubscription, AllStreamSubscriptionOptions,
            ElasticProducer, ElasticProduceOptions>
        ConfigureConnector(
            ConnectorBuilder                                                 cfg,
            ConnectorConfig<EsdbConfig, ElasticConfig>                       config,
            Func<IServiceProvider, IGatewayTransform<ElasticProduceOptions>> getTransform
        ) {
        var serializer       = new RawDataSerializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        return cfg.SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(
                Ensure.NotEmptyString(config.Connector.ConnectorId)
            )
            .ConfigureSubscriptionOptions(
                options => {
                    options.EventSerializer  = serializer;
                    options.ConcurrencyLimit = concurrencyLimit;
                }
            )
            .ConfigureSubscription(
                b => {
                    b.UseCheckpointStore<ElasticCheckpointStore>();
                    b.WithPartitioningByStream(concurrencyLimit);
                }
            )
            .ProduceWith<ElasticProducer, ElasticProduceOptions>()
            .TransformWith(getTransform);
    }
}
