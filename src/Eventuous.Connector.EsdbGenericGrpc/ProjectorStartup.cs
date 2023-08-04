// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Connector.EsdbBase.Config;
using Eventuous.Connector.EsdbGenericGrpc.Config;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Subscriptions.Registrations;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using static Eventuous.Connector.Tools.Ensure;

namespace Eventuous.Connector.EsdbGenericGrpc;

[UsedImplicitly]
public class ProjectorStartup : IConnectorStartup {
    public ConnectorApp BuildConnectorApp(
        string                                  configFile,
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        var builder = ConnectorApp.Create<EsdbConfig, GrpcTargetConfig, NoFilter>(configFile);

        builder.RegisterDependencies(RegisterProject);
        builder.RegisterConnector(ConfigureProjectConnector);

        builder.AddOpenTelemetry(
            (cfg, enrich) => cfg.AddGrpcClientInstrumentation(options => options.Enrich = enrich),
            sampler: new AlwaysOnSampler(),
            tracingExporters: tracingExporters,
            metricsExporters: metricsExporters
        );

        return builder.Build();
    }

    static void RegisterProject(IServiceCollection services, ConnectorConfig<EsdbConfig, GrpcTargetConfig, NoFilter> config)
        => services.AddEventStoreClient(NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string"));

    static ConnectorBuilder<AllStreamSubscription, AllStreamSubscriptionOptions, GrpcJsonProjector, GrpcJsonProjectOptions> ConfigureProjectConnector(
        ConnectorBuilder                                        cfg,
        ConnectorConfig<EsdbConfig, GrpcTargetConfig, NoFilter> config,
        IHealthChecksBuilder                                    healthChecks
    ) {
        var serializer       = new PassThroughSerializer();
        var concurrencyLimit = config.Source.ConcurrencyLimit;

        var builder = cfg
            .SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(NotEmptyString(config.Connector.ConnectorId))
            .ConfigureSubscriptionOptions(options => options.EventSerializer = serializer)
            .ConfigureSubscription(b => b.UseCheckpointStore<GrpcCheckpointStore>().WithPartitioningByStream(concurrencyLimit));

        var getTransform = (IServiceProvider sp) => new DefaultTransform();

        return builder
            .ProduceWith<GrpcJsonProjector, GrpcJsonProjectOptions>(sp => RetryPolicies.RetryForever<RpcException>(sp, config))
            .TransformWith(getTransform);
    }
}
