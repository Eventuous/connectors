// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.Base.Serialization;
using Eventuous.Connector.EsdbBase.Config;
using Eventuous.Connector.Filters.Grpc;
using Eventuous.Connector.Filters.Grpc.Config;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Gateway;
using Eventuous.Producers;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Polly;
using static Eventuous.Connector.Tools.Ensure;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbBase;

public abstract class EsdbProjectorStartup<TConfig, TProjector, TProjectorOptions> : IConnectorStartup
    where TConfig : class
    where TProjector : class, IProducer<TProjectorOptions>
    where TProjectorOptions : class, new() {
    public ConnectorApp BuildConnectorApp(
        string                                  configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp.Create<EsdbConfig, TConfig, GrpcProjectorSettings>(configFile);

        builder.RegisterDependencies(RegisterProject);
        builder.RegisterConnector(ConfigureProjectConnector);

        builder.AddOpenTelemetry(
            (cfg, enrich) => {
                cfg.AddGrpcClientInstrumentation(options => options.EnrichWithHttpRequestMessage = (a, _) => enrich(a));
                ConfigureTrace(cfg, enrich);
            },
            sampler: new AlwaysOnSampler(),
            tracingExporters: tracingExporters,
            metricsExporters: metricsExporters
        );

        return builder.Build();
    }

    protected abstract IGatewayTransform<TProjectorOptions> GetTransform(IServiceProvider serviceProvider);

    ConnectorBuilder<AllStreamSubscription, AllStreamSubscriptionOptions, TProjector, TProjectorOptions> ConfigureProjectConnector(
        ConnectorBuilder                                            builder,
        ConnectorConfig<EsdbConfig, TConfig, GrpcProjectorSettings> config,
        IHealthChecksBuilder                                        healthChecks
    ) {
        var serializer       = new RawDataDeserializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        return builder
            .SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(NotEmptyString(config.Connector.ConnectorId))
            .ConfigureSubscriptionOptions(options => options.EventSerializer = serializer)
            .ConfigureSubscription(
                b => {
                    ConfigureSubscription(b);
                    b.WithPartitioningByStream(concurrencyLimit);
                    b.AddGrpcProjector(NotNull(config.Filter), healthChecks);
                }
            )
            .ProduceWith<TProjector, TProjectorOptions>(sp => GetRetryPolicy(sp, config))
            .TransformWith(GetTransform);
    }

    void RegisterProject(IServiceCollection services, ConnectorConfig<EsdbConfig, TConfig, GrpcProjectorSettings> config) {
        services.AddEventStoreClient(NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string"));
        RegisterTarget(services, config.Target);
    }

    protected abstract IAsyncPolicy GetRetryPolicy(IServiceProvider serviceProvider, ConnectorConfig config);

    protected abstract void ConfigureSubscription(SubscriptionBuilder<AllStreamSubscription, AllStreamSubscriptionOptions> builder);

    protected abstract void RegisterTarget(IServiceCollection services, TConfig config);

    protected abstract void ConfigureTrace(TracerProviderBuilder builder, Action<Activity> enrich);
}