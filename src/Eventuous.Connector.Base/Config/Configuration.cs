// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Eventuous.Connector.Base.Config;

public static class Configuration {
    public static IConfigurationBuilder AddConfiguration(this WebApplicationBuilder builder, string configFile)
        => builder.Configuration.AddYamlFile(configFile, false, true).AddEnvironmentVariables();

    public static ConnectorConfig<TSource, TTarget, TFilter> GetConnectorConfig<TSource, TTarget, TFilter>(this IConfiguration configuration)
        where TSource : class where TTarget : class where TFilter : class
        => configuration.Get<ConnectorConfig<TSource, TTarget, TFilter>>();
}
