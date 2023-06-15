// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Tools;
using Eventuous.Connector.EsdbBase;
using Eventuous.Connector.EsdbSqlServer.Config;
using Eventuous.Connector.Filters.Grpc;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Gateway;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Polly;

// ReSharper disable ConvertToLocalFunction

namespace Eventuous.Connector.EsdbSqlServer;

[UsedImplicitly]
public class ProjectorStartup : EsdbProjectorStartup<SqlConfig, SqlServerProjector, SqlServerProjectOptions> {
    protected override IGatewayTransform<SqlServerProjectOptions> GetTransform(IServiceProvider serviceProvider)
        => new GrpcTransform<SqlServerProjectOptions>(
            "dummy",
            serviceProvider.GetRequiredService<ILogger<GrpcTransform<SqlServerProjectOptions>>>()
        );

    protected override IAsyncPolicy GetRetryPolicy(IServiceProvider serviceProvider, ConnectorConfig config) => RetryPolicies.RetryForever<SqlException>(serviceProvider, config);

    protected override void ConfigureSubscription(SubscriptionBuilder<AllStreamSubscription, AllStreamSubscriptionOptions> builder)
        => builder.UseCheckpointStore<SqlCheckpointStore>();

    protected override void RegisterTarget(IServiceCollection services, SqlConfig config)
        => services.AddSingleton(ConnectionFactory.GetConnectionFactory(Ensure.NotEmptyString(config.ConnectionString, "SQL connection string")));

    protected override void ConfigureTrace(TracerProviderBuilder builder, Action<Activity, string, object> enrich)
        => builder.AddSqlClientInstrumentation(options => options.Enrich = enrich);
}
