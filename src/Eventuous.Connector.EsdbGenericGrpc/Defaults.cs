// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace Eventuous.Connector.EsdbGenericGrpc; 

public class Defaults {
    public static MethodConfig DefaultMethodConfig => new() {
        Names = { MethodName.Default },
        RetryPolicy = new RetryPolicy {
            MaxAttempts          = 20,
            InitialBackoff       = TimeSpan.FromSeconds(1),
            MaxBackoff           = TimeSpan.FromSeconds(5),
            BackoffMultiplier    = 1.5,
            RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.DeadlineExceeded }
        }
    };
    
}
