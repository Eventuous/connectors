using Eventuous;
using Eventuous.Connectors.Base;
using Eventuous.Connector.EsdbElastic.Config;
using Eventuous.Connector.EsdbElastic.Conversions;
using Eventuous.Connector.EsdbElastic.Index;
using Eventuous.Connector.EsdbElastic.Infrastructure;
using Eventuous.Diagnostics.OpenTelemetry;
using Eventuous.ElasticSearch.Producers;
using Eventuous.ElasticSearch.Projections;
using Eventuous.EventStore.Subscriptions;
using Eventuous.Subscriptions.Registrations;
using Nest;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

TypeMap.RegisterKnownEventTypes();
var builder = WebApplication.CreateBuilder();
builder.AddConfiguration();

var config = builder.Configuration.GetConnectorConfig<EsdbConfig, ElasticConfig>();

builder.ConfigureSerilog();

var dataStreamConfig = Ensure.NotNull(config.Target.DataStream);
builder.Services.AddSingleton(dataStreamConfig);

var serializer = new RawDataSerializer();

builder.Services
    .AddSingleton<IEventSerializer>(serializer)
    .AddEventStoreClient(
        Ensure.NotEmptyString(config.Source.ConnectionString, "EventStoreDB connection string")
    )
    .AddElasticClient(config.Target.ConnectionString, config.Target.CloudId, config.Target.ApiKey);

var concurrencyLimit = config.Source.ConcurrencyLimit;
var indexName        = Ensure.NotEmptyString(dataStreamConfig.IndexName);

new ConnectorBuilder()
    .SubscribeWith<AllStreamSubscription, AllStreamSubscriptionOptions>(
        Ensure.NotEmptyString(config.Connector.ConnectorId)
    )
    .ConfigureSubscriptionOptions(
        cfg => {
            cfg.EventSerializer  = serializer;
            cfg.ConcurrencyLimit = concurrencyLimit;
        }
    )
    .ConfigureSubscription(
        b => {
            b.UseCheckpointStore<ElasticCheckpointStore>();
            b.WithPartitioningByStream(concurrencyLimit);
        }
    )
    .ProduceWith<ElasticProducer, ElasticProduceOptions>()
    .TransformWith(_ => new EventTransform(indexName))
    .Register(builder.Services);

builder.AddStartupJob<IElasticClient, IndexConfig>(SetupIndex.CreateIfNecessary);

const string serviceName = "eventuous-connector-esdb-elastic";

builder.Services.AddOpenTelemetryTracing(
    cfg => {
        cfg
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .SetSampler(new TraceIdRatioBasedSampler(1))
            .AddGrpcClientInstrumentation()
            .AddElasticsearchClientInstrumentation()
            .AddEventuousTracing()
            .AddOtlpExporter();
    }
);

builder.Services.AddOpenTelemetryMetrics(
    cfg => cfg
        .AddEventuous()
        .AddEventuousSubscriptions()
        .AddPrometheusExporter()
);
var app = builder.GetHost();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
await app.RunConnector();
