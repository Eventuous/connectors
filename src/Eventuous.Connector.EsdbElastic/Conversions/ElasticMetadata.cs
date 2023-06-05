// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

namespace Eventuous.Connector.EsdbElastic.Conversions; 

public class ElasticMetadata {
    public static Dictionary<string, string?>? FromMetadata(Metadata? metadata)
        => metadata?.ToDictionary(x => x.Key, x => x.Value?.ToString());
}
