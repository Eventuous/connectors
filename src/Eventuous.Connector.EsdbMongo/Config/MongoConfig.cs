// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

namespace Eventuous.Connector.EsdbMongo.Config;

public record MongoConfig {
    public string? ConnectionString { get; [UsedImplicitly] init; }
    public string? Database         { get; [UsedImplicitly] init; }
    public string? Collection       { get; [UsedImplicitly] init; }
    public string  ConnectorMode    { get; init; } = "project";
}
