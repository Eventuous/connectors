using Elasticsearch.Net;
using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.Connector.EsdbElastic.Defaults;
using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.Base.Grpc;
using Eventuous.Connector.Base.Serialization;
using Eventuous.Connector.Base.Tools;
using Eventuous.Connector.EsdbElastic.Conversions;
using Eventuous.ElasticSearch.Index;
using Eventuous.ElasticSearch.Producers;
using Eventuous.ElasticSearch.Projections;
using Eventuous.ElasticSearch.Store;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbElastic;

[UsedImplicitly]
public class ConnectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        string                                  configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp
            .Create<EsdbConfig, ElasticConfig>(configFile);

        if (builder.Config.Target.ConnectorMode == "produce") {
            builder
                .RegisterDependencies(RegisterProduce<PersistedEvent>)
                .RegisterConnector(ConfigureProduceConnector);
        }
        else {
            builder
                .RegisterDependencies(RegisterProject)
                .RegisterConnector(ConfigureProjectConnector);
        }

        builder.AddOpenTelemetry(
            (cfg, enrich) =>
                cfg
                    .AddGrpcClientInstrumentation(options => options.Enrich          = enrich)
                    .AddElasticsearchClientInstrumentation(options => options.Enrich = enrich),
            sampler: new AlwaysOnSampler(),
            tracingExporters: tracingExporters,
            metricsExporters: metricsExporters
        );

        return builder.Build();
    }

    static void RegisterProduce<T>(IServiceCollection services, ConnectorConfig<EsdbConfig, ElasticConfig> config)
        where T : class {
        var dataStreamConfig = Ensure.NotNull(config.Target.DataStream);
        services.AddSingleton(dataStreamConfig);

        var getSerializer = (IElasticsearchSerializer def) => new DefaultElasticSerializer(def);

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

    static void RegisterProject(IServiceCollection services, ConnectorConfig<EsdbConfig, ElasticConfig> config) {
        var getSerializer = (IElasticsearchSerializer def) => new RawDataElasticSerializer(def);

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
    }

    static ConnectorBuilder<
            AllStreamSubscription, AllStreamSubscriptionOptions,
            ElasticProducer, ElasticProduceOptions>
        ConfigureProduceConnector(ConnectorBuilder cfg, ConnectorConfig<EsdbConfig, ElasticConfig> config) {
        var serializer       = new RawDataDeserializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        var indexName    = Ensure.NotEmptyString(config.Target.DataStream?.IndexName);
        var getTransform = (IServiceProvider _) => new DefaultElasticTransform(indexName);

        var builder = cfg.SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(
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
            );

        return builder
            .ProduceWith<ElasticProducer, ElasticProduceOptions>()
            .TransformWith(getTransform);
    }

    static ConnectorBuilder<
            AllStreamSubscription, AllStreamSubscriptionOptions,
            ElasticJsonProjector, ElasticJsonProjectOptions>
        ConfigureProjectConnector(ConnectorBuilder cfg, ConnectorConfig<EsdbConfig, ElasticConfig> config) {
        var serializer       = new RawDataDeserializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        var indexName = Ensure.NotEmptyString(config.Target.DataStream?.IndexName);

        var getTransform =
            (IServiceProvider _) => new GrpcTransform<ElasticJsonProjectOptions>(indexName);

        var builder = cfg.SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(
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
                    b.AddGrpcProjector(config.Grpc, concurrencyLimit);
                }
            );

        return builder
            .ProduceWith<ElasticJsonProjector, ElasticJsonProjectOptions>()
            .TransformWith(getTransform);
    }
}
