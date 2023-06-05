// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Gateway;
using Eventuous.Subscriptions.Context;

namespace Eventuous.Connector.EsdbGenericGrpc;

public class DefaultTransform : IGatewayTransform<GrpcJsonProjectOptions> {
    // readonly ArrayPool<GatewayMessage<GrpcJsonProjectOptions>> _pool = ArrayPool<GatewayMessage<GrpcJsonProjectOptions>>.Shared;

    public ValueTask<GatewayMessage<GrpcJsonProjectOptions>[]> RouteAndTransform(IMessageConsumeContext context) {
        if (context.Message == null) return ValueTask.FromResult(Array.Empty<GatewayMessage<GrpcJsonProjectOptions>>());

        var msg   = new GatewayMessage<GrpcJsonProjectOptions>(context.Stream, context.Message, context.Metadata, GrpcJsonProjectOptions.Default);
        // var array = _pool.Rent(1);
        // array[0] = msg;
        var array = new[] { msg };
        return ValueTask.FromResult(array);
    }
}
