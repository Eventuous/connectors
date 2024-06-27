// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics.CodeAnalysis;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Tools;
using Eventuous.Gateway;
using Eventuous.Producers;
using Eventuous.Subscriptions;
using Eventuous.Subscriptions.Polly;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// ReSharper disable CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

public class ConnectorBuilder {
    [PublicAPI]
    [SuppressMessage("Performance", "CA1822:Mark members as static")]
    public ConnectorBuilder<TSubscription, TSubscriptionOptions> SubscribeWith<TSubscription, TSubscriptionOptions>(string subscriptionId)
        where TSubscription : EventSubscription<TSubscriptionOptions>
        where TSubscriptionOptions : SubscriptionOptions
        => new(subscriptionId);
}

public class ConnectorBuilder<TSub, TSubOptions> : ConnectorBuilder
    where TSub : EventSubscription<TSubOptions>
    where TSubOptions : SubscriptionOptions {
    internal string SubscriptionId { get; }

    internal ConnectorBuilder(string subscriptionId) => SubscriptionId = subscriptionId;

    [PublicAPI]
    public ConnectorBuilder<TSub, TSubOptions> ConfigureSubscriptionOptions(Action<TSubOptions> configureOptions) {
        _configureOptions = configureOptions;

        return this;
    }

    [PublicAPI]
    public ConnectorBuilder<TSub, TSubOptions> ConfigureSubscription(Action<SubscriptionBuilder<TSub, TSubOptions>> configure) {
        _configure = configure;

        return this;
    }

    [PublicAPI]
    public ConnectorBuilder<TSub, TSubOptions, TProducer, TProduceOptions>
        ProduceWith<TProducer, TProduceOptions>(ResolveRetryPolicy? retryPolicy = null, bool awaitProduce = true)
        where TProducer : class, IProducer<TProduceOptions>
        where TProduceOptions : class
        => new(this, retryPolicy, awaitProduce);

    internal void ConfigureOptions(TSubOptions options) => _configureOptions?.Invoke(options);

    internal void Configure(SubscriptionBuilder<TSub, TSubOptions> builder) => _configure?.Invoke(builder);

    Action<TSubOptions>?                            _configureOptions;
    Action<SubscriptionBuilder<TSub, TSubOptions>>? _configure;
}

public class ConnectorBuilder<TSub, TSubOptions, TProducer, TProduceOptions>(
    ConnectorBuilder<TSub, TSubOptions> inner,
    ResolveRetryPolicy?                 resolveRetryPolicy,
    bool                                awaitProduce
)
    where TSub : EventSubscription<TSubOptions>
    where TSubOptions : SubscriptionOptions
    where TProducer : class, IProducer<TProduceOptions>
    where TProduceOptions : class {
    Func<IServiceProvider, IGatewayTransform<TProduceOptions>>? _getTransformer;
    Type?                                                       _transformerType;

    [PublicAPI]
    public ConnectorBuilder<TSub, TSubOptions, TProducer, TProduceOptions> TransformWith<T>(Func<IServiceProvider, T>? getTransformer)
        where T : class, IGatewayTransform<TProduceOptions> {
        _getTransformer  = getTransformer;
        _transformerType = typeof(T);

        return this;
    }

    public void Register(IServiceCollection services, IHealthChecksBuilder healthChecks) {
        services.AddSingleton(
            Ensure.NotNull(_transformerType, "Transformer"),
            Ensure.NotNull(_getTransformer, "GetTransformer")
        );

        services.TryAddSingleton<TProducer>();

        services.AddSubscription<TSub, TSubOptions>(
            inner.SubscriptionId,
            builder => {
                builder.Configure(inner.ConfigureOptions);
                inner.Configure(builder);
                builder.AddEventHandler(GetHandler);
            }
        );
        healthChecks.AddSubscriptionsHealthCheck(inner.SubscriptionId, HealthStatus.Degraded, new[] { inner.SubscriptionId });
        return;

        IEventHandler GetHandler(IServiceProvider sp) {
            var transform = sp.GetRequiredService(_transformerType!) as IGatewayTransform<TProduceOptions>;
            var producer  = sp.GetRequiredService<TProducer>();

            var handler = GatewayHandlerFactory.Create(producer, transform!.RouteAndTransform, awaitProduce);

            return resolveRetryPolicy == null ? handler : new PollyEventHandler(handler, resolveRetryPolicy(sp));
        }
    }
}
