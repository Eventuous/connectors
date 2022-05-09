using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.Base.Grpc;
using Eventuous.Connector.Base.Serialization;
using Eventuous.Connector.EsdbSqlServer.Config;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbSqlServer;

[UsedImplicitly]
public class ConnectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        string                                  configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp
            .Create<EsdbConfig, SqlConfig>(configFile);

        builder
            .RegisterDependencies(RegisterProject)
            .RegisterConnector(ConfigureProjectConnector);

        builder.AddOpenTelemetry(
            (cfg, enrich) =>
                cfg
                    .AddGrpcClientInstrumentation(options => options.Enrich = enrich)
                    .AddSqlClientInstrumentation(options => options.Enrich  = enrich),
            sampler: new AlwaysOnSampler(),
            tracingExporters: tracingExporters,
            metricsExporters: metricsExporters
        );

        return builder.Build();
    }

    static void RegisterProject(IServiceCollection services, ConnectorConfig<EsdbConfig, SqlConfig> config)
        => services
            .AddEventStoreClient(
                Ensure.NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string")
            )
            .AddSingleton(
                ConnectionFactory.GetConnectionFactory(
                    Ensure.NotEmptyString(config.Target.ConnectionString, "SQL connection string")
                )
            );

    static ConnectorBuilder<
            AllStreamSubscription, AllStreamSubscriptionOptions,
            SqlProjector, SqlServerProjectOptions>
        ConfigureProjectConnector(ConnectorBuilder cfg, ConnectorConfig<EsdbConfig, SqlConfig> config) {
        var serializer       = new RawDataDeserializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        var getTransform =
            (IServiceProvider _) => new GrpcTransform<SqlServerProjectOptions>("dummy");

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
                    b.UseCheckpointStore<SqlCheckpointStore>();
                    b.WithPartitioningByStream(concurrencyLimit);

                    var grpcUri = Ensure.NotEmptyString(config.Grpc?.Uri, "gRPC projector URI");
                    b.AddConsumeFilterLast(
                        new GrpcProjectionFilter(grpcUri)
                    );
                }
            );

        return builder
            .ProduceWith<SqlProjector, SqlServerProjectOptions>()
            .TransformWith(getTransform);
    }
}
