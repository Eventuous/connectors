// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Tools;
using Eventuous.Connector.EsdbBase;
using Eventuous.Connector.EsdbMongo.Config;
using Eventuous.Connector.Filters.Grpc;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Gateway;
using Eventuous.Projections.MongoDB;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OpenTelemetry.Trace;
using Polly;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbMongo;

[UsedImplicitly]
public class ProjectorStartup : EsdbProjectorStartup<MongoConfig, MongoJsonProjector, MongoJsonProjectOptions> {
    protected override IGatewayTransform<MongoJsonProjectOptions> GetTransform(IServiceProvider serviceProvider) {
        var config = serviceProvider.GetRequiredService<MongoConfig>();

        return new GrpcTransform<MongoJsonProjectOptions>(
            Ensure.NotEmptyString(config.Collection, "MongoDB collection"),
            serviceProvider.GetRequiredService<ILogger<GrpcTransform<MongoJsonProjectOptions>>>()
        );
    }

    protected override IAsyncPolicy GetRetryPolicy(IServiceProvider serviceProvider, ConnectorConfig config)
        => RetryPolicies.RetryForever<MongoConnectionException>(serviceProvider, config);

    protected override void ConfigureSubscription(SubscriptionBuilder<AllStreamSubscription, AllStreamSubscriptionOptions> builder)
        => builder.UseCheckpointStore<MongoCheckpointStore>();

    protected override string GetTarget(MongoConfig config) => Ensure.NotEmptyString(config.Collection, "MongoDB collection");

    protected override void RegisterTarget(IServiceCollection services, MongoConfig config)
        => services.AddMongo(
            Ensure.NotEmptyString(config.ConnectionString, "MongoDB connection string"),
            Ensure.NotEmptyString(config.Database, "MongoDB database")
        );

    protected override void ConfigureTrace(TracerProviderBuilder builder, Action<Activity, string, object> enrich) => builder.AddMongoDBInstrumentation();
}
