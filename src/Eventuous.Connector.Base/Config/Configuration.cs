using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Eventuous.Connector.Base.Config;

public static class Configuration {
    public static IConfigurationBuilder AddConfiguration(this WebApplicationBuilder builder, string configFile)
        => builder.Configuration.AddYamlFile(configFile, false, true).AddEnvironmentVariables();

    public static ConnectorConfig<TSource, TTarget>
        GetConnectorConfig<TSource, TTarget>(this IConfiguration configuration)
        where TSource : class where TTarget : class
        => configuration.Get<ConnectorConfig<TSource, TTarget>>();
}
