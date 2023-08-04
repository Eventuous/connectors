// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;
using Eventuous.Connector.Base.Grpc;
using Eventuous.Connector.Filters.Grpc.Extensions;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Eventuous.Connector.Filters.Grpc;

public sealed class GrpcProjectionFilter : ConsumeFilter<AsyncConsumeContext>, IHealthCheck, IAsyncDisposable {
    GrpcPartition?[]                 _partitions = Array.Empty<GrpcPartition>();
    readonly Func<GrpcPartition>     _partitionFactory;
    readonly CancellationTokenSource _cts;

    public GrpcProjectionFilter(string host, ChannelCredentials credentials) {
        _cts = new CancellationTokenSource();
        _partitionFactory = () => new GrpcPartition(host, credentials, _cts);
    }

    readonly object _lock = new();

    protected override async ValueTask Send(AsyncConsumeContext context, LinkedListNode<IConsumeFilter>? next) {
        var (projector, responseHandler) = GetPartition();
        var json = responseHandler.Prepare(context, next);

        await projector.Project(
            new ProjectionRequest {
                Stream = context.Stream,
                EventType = context.MessageType,
                EventId = context.MessageId,
                EventPayload = Struct.Parser.ParseJson(json)
            }
        );

        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        GrpcPartition GetPartition() {
            if (_partitions.Length <= context.PartitionId) {
                lock (_lock) {
                    Array.Resize(ref _partitions, (int)(context.PartitionId + 1));
                    _partitions[context.PartitionId] = _partitionFactory();
                }
            }
            else if (_partitions[context.PartitionId] is null) {
                lock (_lock) {
                    _partitions[context.PartitionId] = _partitionFactory();
                }
            }

            return _partitions[context.PartitionId]!;
        }
    }

    public async ValueTask DisposeAsync() {
        _cts.Cancel();
        await _partitions.Where(x => x != null).Select(x => x!.Projector.DisposeAsync()).WhenAll();
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        var status = HealthCheckResult.Healthy();

        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < _partitions.Length; i++) {
            var partitionProjector = _partitions[i]?.Projector;
            if (partitionProjector == null) continue;

            var partitionStatus = partitionProjector.GetStatus();
            if (partitionStatus.Status < status.Status) {
                status = partitionStatus;
            }
        }

        return Task.FromResult(status);
    }

    record GrpcPartition {
        public GrpcPartition(string host, ChannelCredentials credentials, CancellationTokenSource cts) {
            ResponseHandler = new GrpcResponseHandler();
            Projector = new Projector(host, credentials, ResponseHandler.Handler);
            Projector.Run(cts.Token);
        }

        public Projector           Projector       { get; }
        public GrpcResponseHandler ResponseHandler { get; }

        public void Deconstruct(out Projector projector, out GrpcResponseHandler responseHandler) {
            projector = Projector;
            responseHandler = ResponseHandler;
        }
    }
}
