// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.ElasticSearch.Index;

namespace Eventuous.Connector.EsdbElastic.Config;

public record ElasticConfig {
    public string?      ConnectionString { get; init; }
    public string?      CloudId          { get; init; }
    public string?      ApiKey           { get; init; }
    public IndexConfig? DataStream       { get; init; }
    public string       ConnectorMode    { get; init; } = "produce";
}
