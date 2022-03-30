using Elasticsearch.Net;
using Eventuous.Connectors.EsdbElastic.Conversions;
using Nest;
using static System.String;

namespace Eventuous.Connectors.EsdbElastic.Infrastructure;

static class RegistrationExtensions {
    public static IServiceCollection AddElasticClient(
        this IServiceCollection                       services,
        string?                                       connectionString,
        string?                                       cloudId,
        string?                                       apiKey,
        Func<ConnectionSettings, ConnectionSettings>? configureSettings = null
    )
        => services.AddSingleton<IElasticClient>(
            CreateElasticClient(connectionString, cloudId, apiKey, configureSettings)
        );

    static ElasticClient CreateElasticClient(
        string?                                       connectionString,
        string?                                       cloudId,
        string?                                       apiKey,
        Func<ConnectionSettings, ConnectionSettings>? configureSettings = null
    ) {
        var settings = cloudId != null
            ? new ConnectionSettings(cloudId, new ApiKeyAuthenticationCredentials(apiKey))
            : new ConnectionSettings(
                new SingleNodeConnectionPool(
                    new Uri(Ensure.NotEmptyString(connectionString, "Elasticsearch connection string"))
                ),
                (def, _) => new ElasticSerializer(def)
            );

        if (configureSettings is not null) settings = configureSettings(settings);

        return IsNullOrEmpty(apiKey)
            ? new ElasticClient(settings)
            : new ElasticClient(settings.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(apiKey)));
    }
}
