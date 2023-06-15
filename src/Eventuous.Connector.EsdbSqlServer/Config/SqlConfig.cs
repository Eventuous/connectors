// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

namespace Eventuous.Connector.EsdbSqlServer.Config;

public record SqlConfig {
    public string ConnectionString { get; init; } = null!;
}
