// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.ElasticSearch.Index;

namespace Eventuous.Connector.EsdbElastic.Config;

public record ElasticConfig {
    public string?      ConnectionString { get; [UsedImplicitly] init; }
    public string?      CloudId          { get; [UsedImplicitly] init; }
    public string?      ApiKey           { get; [UsedImplicitly] init; }
    public IndexConfig? DataStream       { get; [UsedImplicitly] init; }
    public string       ConnectorMode    { get; [UsedImplicitly] init; } = "produce";
}
