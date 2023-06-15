// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Eventuous.Connector.EsdbBase.Config;

public record EsdbConfig {
    public string ConnectionString { get; init; } = null!;
    public int    ConcurrencyLimit { get; init; } = 1;
}
