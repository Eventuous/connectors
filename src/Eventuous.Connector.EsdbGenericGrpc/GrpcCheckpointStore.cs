// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector.Base.Config;
using Eventuous.Connector.EsdbGenericGrpc.Config;
using Eventuous.Subscriptions.Checkpoints;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Logging;
using Polly;

namespace Eventuous.Connector.EsdbGenericGrpc;

public class GrpcCheckpointStore : ICheckpointStore {
    readonly ILogger<GrpcCheckpointStore> _log;
    readonly string                       _host;
    readonly ChannelCredentials           _creds;

    static readonly AsyncPolicy DefaultRetryPolicy = Policy
        .Handle<RpcException>()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

    public GrpcCheckpointStore(GrpcTargetConfig config, ILogger<GrpcCheckpointStore> log) {
        _log = log;
        _host = config.GetHost();
        _creds = config.GetCredentials();
    }

    GrpcChannel GetChannel()
        => GrpcChannel.ForAddress(
            _host,
            new GrpcChannelOptions {
                Credentials = _creds,
                ServiceConfig = new ServiceConfig { MethodConfigs = { Defaults.DefaultMethodConfig } }
            }
        );

    public async ValueTask<Checkpoint> GetLastCheckpoint(string checkpointId, CancellationToken cancellationToken) {
        using var channel = GetChannel();

        var client = new Projection.ProjectionClient(channel);

        var response = await client.GetCheckpointAsync(
            new GetCheckpointRequest { CheckpointId = checkpointId },
            cancellationToken: cancellationToken
        );

        _log.LogInformation("[{CheckpointId}] Received checkpoint at {Position}", checkpointId, response.Position);

        var position = response.CheckpointCase == GetCheckpointResponse.CheckpointOneofCase.None ? null : (ulong?)response.Position;
        return new Checkpoint(checkpointId, position);
    }

    public async ValueTask<Checkpoint> StoreCheckpoint(Checkpoint checkpoint, bool force, CancellationToken cancellationToken) {
        using var channel = GetChannel();

        var client = new Projection.ProjectionClient(channel);

        await client.StoreCheckpointAsync(
            new StoreCheckpointRequest {
                Position = checkpoint.Position.HasValue ? (long)checkpoint.Position.Value : 0,
                CheckpointId = checkpoint.Id
            },
            cancellationToken: cancellationToken
        );

        _log.LogInformation("[{CheckpointId}] Stored checkpoint at {Position}", checkpoint.Id, checkpoint.Position ?? 0);

        return checkpoint;
    }
}
