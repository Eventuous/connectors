// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Serilog;

namespace Eventuous.Connector.Base.Grpc;

public sealed class Projector : IAsyncDisposable {
    static readonly ILogger Log = Serilog.Log.ForContext<Projector>();

    readonly MethodConfig _defaultMethodConfig = new() {
        Names = { MethodName.Default },
        RetryPolicy = new RetryPolicy {
            MaxAttempts = 20,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(5),
            BackoffMultiplier = 1.5,
            RetryableStatusCodes = { StatusCode.Unavailable }
        }
    };

    readonly Projection.ProjectionClient                       _client;
    readonly Func<ProjectionResponse, CancellationToken, Task> _handler;

    Task?                    _readTask;
    CancellationTokenSource? _cts;
    CancellationToken        _ct;

    AsyncDuplexStreamingCall<ProjectionRequest, ProjectionResponse>? _call;

    public Projector(
        string                                            host,
        ChannelCredentials                                credentials,
        Func<ProjectionResponse, CancellationToken, Task> handler
    ) {
        _handler = handler;

        var channel = GrpcChannel.ForAddress(
            host,
            new GrpcChannelOptions {
                Credentials = credentials,
                ServiceConfig = new ServiceConfig { MethodConfigs = { _defaultMethodConfig } }
            }
        );

        _client = new Projection.ProjectionClient(channel);
    }

    public void Run(CancellationToken cancellationToken) {
        _cts = new CancellationTokenSource();
        _ct = cancellationToken;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, _ct);

        _call = _client.Project(cancellationToken: linked.Token);

        _call.RequestStream.WriteOptions = new WriteOptions(WriteFlags.BufferHint);

        async Task HandleResponses() {
            Log.Information("Subscribing...");

            await foreach (var response in _call.ResponseStream.ReadAllAsync(cancellationToken: linked.Token)) {
                // add retries
                Log.Verbose("Received response: {Response}", response);
                await _handler(response, linked.Token);
            }
        }

        _readTask = Task.Run(HandleResponses, linked.Token);
    }

    async Task Resubscribe() {
        if (_disposing) {
            return;
        }

        _cts?.Cancel();
        _call?.Dispose();

        if (_readTask != null) {
            try {
                await AwaitCancelled(() => _readTask);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable) {
                Log.Warning("Server unavailable");
            }

            _readTask.Dispose();
            _readTask = null;
        }

        _cts = null;
        _call = null;
        Run(_ct);
    }

    public async Task Project(ProjectionRequest projectionContext) {
        var retry = 100;

        while (retry-- > 0 && !_disposing) {
            var r = await ProjectInternal();

            if (r == ProjectResult.Ok) {
                break;
            }

            Log.Information($"Retrying {100 - retry}");
        }

        async Task<ProjectResult> ProjectInternal() {
            try {
                await _call!.RequestStream.WriteAsync(projectionContext, _ct);
                return ProjectResult.Ok;
            }
            catch (InvalidOperationException e)
                when (e.Message.Contains("previous write is in progress")) {
                // TODO: this is a hack, it needs to open multiple streams for concurrent projectors
                Log.Warning("Write already in progress");
                return ProjectResult.Retry;
            }
            catch (ObjectDisposedException) {
                await Resubscribe();
                return ProjectResult.Retry;
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable) {
                await Resubscribe();
                return ProjectResult.Retry;
            }
            catch (Exception e) {
                Log.Error(e, "Projection failed");
                throw;
            }
        }
    }

    bool _disposing;

    public async ValueTask DisposeAsync() {
        if (_disposing) {
            return;
        }

        _disposing = true;
        Log.Information("Disconnecting...");
        _cts?.Cancel();

        if (_call != null) {
            Log.Information("Closing the request stream...");
            await AwaitCancelled(() => _call.RequestStream.CompleteAsync());
        }

        if (_readTask != null) {
            Log.Information("Closing the reader...");
            await AwaitCancelled(() => _readTask);
        }
    }

    static async Task AwaitCancelled(Func<Task> action) {
        try {
            await action();
        }
        catch (OperationCanceledException) { }
        catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled) { }
    }
}

public enum ProjectResult {
    Ok,
    Retry
}
