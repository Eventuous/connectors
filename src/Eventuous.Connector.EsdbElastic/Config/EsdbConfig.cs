// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

namespace Eventuous.Connector.EsdbElastic.Config;

public record EsdbConfig {
    public string ConnectionString { get; [UsedImplicitly] init; } = null!;
    public int    ConcurrencyLimit { get; [UsedImplicitly] init; } = 1;
}
