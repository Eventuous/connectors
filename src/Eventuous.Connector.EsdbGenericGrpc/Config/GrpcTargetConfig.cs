// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Grpc.Core;
using static Eventuous.Connector.Tools.Ensure;

namespace Eventuous.Connector.EsdbGenericGrpc.Config;

public record GrpcTargetConfig {
    // TODO: Add credentials
    public string Uri         { get; init; } = "http://localhost:9200";
    public string Credentials { get; init; } = "ssl";

    public string GetHost() => NotEmptyString(Uri, "gRPC projector URI");

    public ChannelCredentials GetCredentials() {
        var setting = NotEmptyString(Credentials, "gRPC projector credentials");

        return setting switch {
            "insecure" => ChannelCredentials.Insecure,
            "ssl"      => ChannelCredentials.SecureSsl,
            _          => throw new ArgumentOutOfRangeException(setting, "Unknown credentials")
        };
    }
}
