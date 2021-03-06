using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.Base.Grpc;
using Eventuous.Connector.Base.Serialization;
using Eventuous.Connector.Base.Tools;
using Eventuous.Connector.EsdbMongo.Config;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbMongo;

[UsedImplicitly]
public class ConnectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        string                                  configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp
            .Create<EsdbConfig, MongoConfig>(configFile);

        builder
            .RegisterDependencies(RegisterProject)
            .RegisterConnector(ConfigureProjectConnector);

        builder.AddOpenTelemetry(
            (cfg, enrich) =>
                cfg
                    .AddGrpcClientInstrumentation(options => options.Enrich = enrich)
                    .AddMongoDBInstrumentation(),
            sampler: new AlwaysOnSampler(),
            tracingExporters: tracingExporters,
            metricsExporters: metricsExporters
        );

        return builder.Build();
    }

    static void RegisterProject(IServiceCollection services, ConnectorConfig<EsdbConfig, MongoConfig> config)
        => services
            .AddEventStoreClient(
                Ensure.NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string")
            )
            .AddMongo(
                Ensure.NotEmptyString(config.Target.ConnectionString, "MongoDB connection string"),
                Ensure.NotEmptyString(config.Target.Database, "MongoDB database")
            );

    static ConnectorBuilder<
            AllStreamSubscription, AllStreamSubscriptionOptions,
            MongoJsonProjector, MongoJsonProjectOptions>
        ConfigureProjectConnector(ConnectorBuilder cfg, ConnectorConfig<EsdbConfig, MongoConfig> config) {
        var serializer       = new RawDataDeserializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        var getTransform =
            (IServiceProvider _) => new GrpcTransform<MongoJsonProjectOptions>(
                Ensure.NotEmptyString(config.Target.Collection, "MongoDB collection")
            );

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
                    b.UseCheckpointStore<MongoCheckpointStore>();
                    b.WithPartitioningByStream(concurrencyLimit);
                    b.AddGrpcProjector(config.Grpc);
                }
            );

        var retryPolicy = Policy
            .Handle<MongoConnectionException>()
            .WaitAndRetryForeverAsync(
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt * 100))
            );
        return builder
            .ProduceWith<MongoJsonProjector, MongoJsonProjectOptions>(retryPolicy)
            .TransformWith(getTransform);
    }
}
