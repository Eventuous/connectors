using Elasticsearch.Net;
using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.Connector.EsdbElastic.Conversions;
using Eventuous.Connector.EsdbElastic.Infrastructure;
using Eventuous.Connectors.Base;
using Eventuous.ElasticSearch.Index;
using Eventuous.ElasticSearch.Producers;
using Eventuous.ElasticSearch.Projections;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Gateway;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Nest;

namespace Eventuous.Connector.EsdbElastic;

public static class Startup {
    public static void Register<T>(
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

    public static ConnectorBuilder<
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
