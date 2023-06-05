// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Elasticsearch.Net;
using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.Connector.EsdbElastic.Defaults;
using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.Base.Serialization;
using Eventuous.Connector.EsdbElastic.Conversions;
using Eventuous.Connector.Filters.Grpc;
using Eventuous.Connector.Filters.Grpc.Config;
using Eventuous.ElasticSearch.Index;
using Eventuous.ElasticSearch.Producers;
using Eventuous.ElasticSearch.Projections;
using Eventuous.ElasticSearch.Store;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using static Eventuous.Connector.Tools.Ensure;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbElastic;

[UsedImplicitly]
public class ConnectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        string                                  configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp.Create<EsdbConfig, ElasticConfig, GrpcProjectorSettings>(configFile);

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
                    .AddGrpcClientInstrumentation(options => options.Enrich = enrich)
                    .AddElasticsearchClientInstrumentation(options => options.Enrich = enrich),
            sampler: new AlwaysOnSampler(),
            tracingExporters: tracingExporters,
            metricsExporters: metricsExporters
        );

        return builder.Build();
    }

    static void RegisterProduce<T>(IServiceCollection services, ConnectorConfig<EsdbConfig, ElasticConfig, GrpcProjectorSettings> config)
        where T : class {
        var dataStreamConfig = NotNull(config.Target.DataStream);
        services.AddSingleton(dataStreamConfig);

        var getSerializer = (IElasticsearchSerializer def) => new DefaultElasticSerializer(def);
        RegisterDependencies(services, config, getSerializer);
        services.AddStartupJob<IElasticClient, IndexConfig>(SetupIndex.CreateIndexIfNecessary<T>);
    }

    static void RegisterProject(IServiceCollection services, ConnectorConfig<EsdbConfig, ElasticConfig, GrpcProjectorSettings> config) {
        var getSerializer = (IElasticsearchSerializer def) => new RawDataElasticSerializer(def);
        RegisterDependencies(services, config, getSerializer);
    }

    static void RegisterDependencies(
        IServiceCollection                                                services,
        ConnectorConfig<EsdbConfig, ElasticConfig, GrpcProjectorSettings> config,
        Func<IElasticsearchSerializer, IElasticsearchSerializer>?         getSerializer
    )
        => services
            .AddEventStoreClient(NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string"))
            .AddElasticClient(config.Target.ConnectionString, config.Target.CloudId, config.Target.ApiKey, getSerializer);

    static ConnectorBuilder<AllStreamSubscription, AllStreamSubscriptionOptions, ElasticProducer, ElasticProduceOptions> ConfigureProduceConnector(
        ConnectorBuilder                                                  cfg,
        ConnectorConfig<EsdbConfig, ElasticConfig, GrpcProjectorSettings> config
    ) {
        var indexName    = NotEmptyString(config.Target.DataStream?.IndexName);
        var getTransform = (IServiceProvider _) => new DefaultElasticTransform(indexName);
        var builder      = AddSubscription(cfg, config, null);

        return builder
            .ProduceWith<ElasticProducer, ElasticProduceOptions>()
            .TransformWith(getTransform);
    }

    static ConnectorBuilder<AllStreamSubscription, AllStreamSubscriptionOptions, ElasticJsonProjector, ElasticJsonProjectOptions> ConfigureProjectConnector(
        ConnectorBuilder                                                  cfg,
        ConnectorConfig<EsdbConfig, ElasticConfig, GrpcProjectorSettings> config
    ) {
        var indexName = NotEmptyString(config.Target.DataStream?.IndexName);

        var getTransform =
            (IServiceProvider sp) => new GrpcTransform<ElasticJsonProjectOptions>(
                indexName,
                sp.GetRequiredService<ILogger<GrpcTransform<ElasticJsonProjectOptions>>>()
            );

        var builder = AddSubscription(cfg, config, b => b.AddGrpcProjector(NotNull(config.Filter)));

        return builder
            .ProduceWith<ElasticJsonProjector, ElasticJsonProjectOptions>()
            .TransformWith(getTransform);
    }

    static ConnectorBuilder<AllStreamSubscription, AllStreamSubscriptionOptions> AddSubscription(
        ConnectorBuilder                                                                  cfg,
        ConnectorConfig<EsdbConfig, ElasticConfig, GrpcProjectorSettings>                 config,
        Action<SubscriptionBuilder<AllStreamSubscription, AllStreamSubscriptionOptions>>? configureSubscription
    ) {
        var serializer       = new RawDataDeserializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        return cfg.SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(NotEmptyString(config.Connector.ConnectorId))
            .ConfigureSubscriptionOptions(options => options.EventSerializer = serializer)
            .ConfigureSubscription(
                b => {
                    b.UseCheckpointStore<ElasticCheckpointStore>();
                    b.WithPartitioningByStream(concurrencyLimit);
                    configureSubscription?.Invoke(b);
                }
            );
    }
}
