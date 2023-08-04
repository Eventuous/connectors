// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace Eventuous.Connector.EsdbMongo;

public static class MongoRegistrationExtensions {
    public static IServiceCollection AddMongo(this IServiceCollection services, string connectionString, string database)
        => services.AddSingleton(ConfigureMongo(connectionString, database));

    public static IMongoDatabase ConfigureMongo(string connectionString, string database) {
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());

        return new MongoClient(settings).GetDatabase(database);
    }
}
