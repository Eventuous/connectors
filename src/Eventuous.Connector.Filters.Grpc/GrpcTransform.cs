// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Gateway;
using Eventuous.Subscriptions.Context;
using Microsoft.Extensions.Logging;

namespace Eventuous.Connector.Filters.Grpc;

public class GrpcTransform<TProduceOptions> : IGatewayTransform<TProduceOptions> where TProduceOptions : new() {
    readonly string  _target;
    readonly ILogger _log;

    public GrpcTransform(string target, ILogger log) {
        _target = target;
        _log = log;
    }

    static readonly TProduceOptions Options = new();

    public ValueTask<GatewayMessage<TProduceOptions>[]> RouteAndTransform(IMessageConsumeContext context) {
        if (!context.Items.TryGetItem<ProjectionResponse>(GrpcContextKeys.ProjectionResult, out var projectionResult)) {
            _log.LogWarning("Projection result not found in context, stream {Stream}, message {MessageId}", context.Stream, context.MessageId);
            return ValueTask.FromResult(Array.Empty<GatewayMessage<TProduceOptions>>());
        }

        var gatewayMessage = new GatewayMessage<TProduceOptions>(new StreamName(_target), projectionResult!, null, Options);

        return ValueTask.FromResult(new[] { gatewayMessage });
    }
}
