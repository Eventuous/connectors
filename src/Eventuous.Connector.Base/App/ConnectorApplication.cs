// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using Eventuous.Diagnostics;
using Eventuous.Diagnostics.OpenTelemetry;
using Eventuous.Producers;
using Eventuous.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

namespace Eventuous.Connector.Base.App;

public class ConnectorApplicationBuilder<TSourceConfig, TTargetConfig, TFilterConfig>
    where TSourceConfig : class
    where TTargetConfig : class
    where TFilterConfig : class {
    public delegate void ResolveDependencies(IServiceCollection services, ConnectorConfig<TSourceConfig, TTargetConfig, TFilterConfig> config);

    public delegate ConnectorBuilder<TSubscription, TSubscriptionOptions, TProducer, TProduceOptions> ConfigureConnector<TSubscription, TSubscriptionOptions,
        TProducer, TProduceOptions>(
        ConnectorBuilder                                             builder,
        ConnectorConfig<TSourceConfig, TTargetConfig, TFilterConfig> config,
        IHealthChecksBuilder                                         healthChecks
    )
        where TSubscription : EventSubscription<TSubscriptionOptions>
        where TSubscriptionOptions : SubscriptionOptions
        where TProducer : class, IEventProducer<TProduceOptions>
        where TProduceOptions : class;

    LogEventLevel?                                      _minimumLogLevel;
    Func<LoggerSinkConfiguration, LoggerConfiguration>? _sinkConfiguration;
    Func<LoggerConfiguration, LoggerConfiguration>?     _configureLogger;

    internal ConnectorApplicationBuilder(string configFile) {
        Builder = WebApplication.CreateBuilder();
        Builder.AddConfiguration(configFile);
        Config = Builder.Configuration.GetConnectorConfig<TSourceConfig, TTargetConfig, TFilterConfig>();
        Builder.Services.AddSingleton(Config.Source);
        Builder.Services.AddSingleton(Config.Target);
        HealthChecks = Builder.Services.AddHealthChecks();

        if (!Config.Connector.Diagnostics.Enabled) { Environment.SetEnvironmentVariable("EVENTUOUS_DISABLE_DIAGS", "1"); }

        var logLevel = Logging.ParseLogLevel(Config.Connector.LogLevel);
        ConfigureSerilog(logLevel);
    }

    public IHealthChecksBuilder HealthChecks { get; }
    WebApplicationBuilder       Builder      { get; }

    public ConnectorConfig<TSourceConfig, TTargetConfig, TFilterConfig> Config { get; }

    [PublicAPI]
    public void ConfigureSerilog(
        LogEventLevel?                                      minimumLogLevel   = null,
        Func<LoggerSinkConfiguration, LoggerConfiguration>? sinkConfiguration = null,
        Func<LoggerConfiguration, LoggerConfiguration>?     configureLogger   = null
    ) {
        _minimumLogLevel   = minimumLogLevel;
        _sinkConfiguration = sinkConfiguration;
        _configureLogger   = configureLogger;
    }

    public void RegisterDependencies(ResolveDependencies configure) => configure(Builder.Services, Config);

    public void RegisterConnector<TSubscription, TSubscriptionOptions, TProducer, TProduceOptions>(
        ConfigureConnector<TSubscription, TSubscriptionOptions, TProducer, TProduceOptions> configure
    )
        where TSubscription : EventSubscription<TSubscriptionOptions>
        where TSubscriptionOptions : SubscriptionOptions
        where TProducer : class, IEventProducer<TProduceOptions>
        where TProduceOptions : class {
        var builder = configure(new ConnectorBuilder(), Config, HealthChecks);
        builder.Register(Builder.Services, HealthChecks);
    }

    const string ConnectorIdTag = "connectorId";

    void EnrichActivity(Activity activity, string arg1, object arg2) => activity.AddTag(ConnectorIdTag, Config.Connector.ConnectorId);

    bool _oTelAdded;

    [PublicAPI]
    public void AddOpenTelemetry(
        Action<TracerProviderBuilder, Action<Activity, string, object>>? configureTracing = null,
        Action<MeterProviderBuilder>?                                    configureMetrics = null,
        Sampler?                                                         sampler          = null,
        ExporterMappings<TracerProviderBuilder>?                         tracingExporters = null,
        ExporterMappings<MeterProviderBuilder>?                          metricsExporters = null
    ) {
        _oTelAdded = true;

        if (!Config.Connector.Diagnostics.Enabled) { return; }

        EventuousDiagnostics.AddDefaultTag(ConnectorIdTag, Config.Connector.ConnectorId);

        var oTelBuilder = Builder.Services.AddOpenTelemetry();

        if (Config.Connector.Diagnostics.Tracing is { Enabled: true }) {
            oTelBuilder.WithTracing(
                cfg => {
                    cfg.AddEventuousTracing();

                    configureTracing?.Invoke(cfg, EnrichActivity);

                    cfg.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Config.Connector.ServiceName))
                        .SetSampler(sampler ?? new TraceIdRatioBasedSampler(Config.Connector.Diagnostics.TraceSamplerProbability));

                    tracingExporters?.RegisterExporters(cfg, Config.Connector.Diagnostics.Tracing.Exporters);
                }
            );
        }

        if (Config.Connector.Diagnostics.Metrics is { Enabled: true }) {
            oTelBuilder.WithMetrics(
                cfg => {
                    cfg.AddEventuous().AddEventuousSubscriptions();
                    configureMetrics?.Invoke(cfg);
                    cfg.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Config.Connector.ServiceName));
                    metricsExporters?.RegisterExporters(cfg, Config.Connector.Diagnostics.Metrics.Exporters);
                }
            );
        }
    }

    public ConnectorApp Build() {
        Builder.ConfigureSerilog(_minimumLogLevel, _sinkConfiguration, _configureLogger);

        Builder.Services.AddHealthChecks().AddSubscriptionsHealthCheck("Subscriptions", HealthStatus.Unhealthy, new[] { Config.Connector.ConnectorId });

        if (!_oTelAdded) { AddOpenTelemetry(); }

        var app = Builder.Build();

        return new ConnectorApp(app);
    }
}

public class ConnectorApp {
    [PublicAPI]
    public static ConnectorApplicationBuilder<TSourceConfig, TTargetConfig, TFilterConfig> Create<TSourceConfig, TTargetConfig, TFilterConfig>(
        string configFile
    )
        where TSourceConfig : class
        where TTargetConfig : class
        where TFilterConfig : class
        => new(configFile);

    public WebApplication Host { get; }

    internal ConnectorApp(WebApplication host) => Host = host;

    public async Task<int> Run() {
        Host.MapHealthChecks("/health");

        try {
            await Host.RunConnector();

            return 0;
        } catch (Exception ex) {
            Log.Fatal(ex, "Host terminated unexpectedly");

            return -1;
        } finally { await Log.CloseAndFlushAsync(); }
    }
}

public static class ConnectorBuilderExtensions {
    [PublicAPI]
    public static Task RunConnector<TSourceConfig, TTargetConfig, TFilterConfig>(
        this ConnectorApplicationBuilder<TSourceConfig, TTargetConfig, TFilterConfig> builder
    )
        where TSourceConfig : class
        where TTargetConfig : class
        where TFilterConfig : class {
        var application = builder.Build();

        return application.Run();
    }
}
