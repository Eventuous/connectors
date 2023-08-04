// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

namespace Eventuous.Connector.Filters.Grpc;

public sealed class Projector : IAsyncDisposable {
    static readonly ILogger Log = Serilog.Log.ForContext<Projector>();

    readonly MethodConfig _defaultMethodConfig = new() {
        Names = { MethodName.Default },
        RetryPolicy = new RetryPolicy {
            MaxAttempts          = 20,
            InitialBackoff       = TimeSpan.FromSeconds(1),
            MaxBackoff           = TimeSpan.FromSeconds(5),
            BackoffMultiplier    = 1.5,
            RetryableStatusCodes = { StatusCode.Unavailable }
        }
    };

    readonly Projection.ProjectionClient                       _client;
    readonly Func<ProjectionResponse, CancellationToken, Task> _handler;

    Task?                    _readTask;
    CancellationTokenSource? _cts;
    CancellationToken        _ct;
    HealthCheckResult        _status;
    string                   _host;

    AsyncDuplexStreamingCall<ProjectionRequest, ProjectionResponse>? _call;

    public Projector(
        string                                            host,
        ChannelCredentials                                credentials,
        Func<ProjectionResponse, CancellationToken, Task> handler
    ) {
        _handler = handler;
        _host    = host;

        var channel = GrpcChannel.ForAddress(
            host,
            new GrpcChannelOptions {
                Credentials   = credentials,
                ServiceConfig = new ServiceConfig { MethodConfigs = { _defaultMethodConfig } }
            }
        );

        _client = new Projection.ProjectionClient(channel);
        _status = HealthCheckResult.Healthy(host);
    }

    public void Run(CancellationToken cancellationToken) {
        _cts = new CancellationTokenSource();
        _ct  = cancellationToken;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, _ct);

        _call = _client.Project(cancellationToken: linked.Token);

        _call.RequestStream.WriteOptions = new WriteOptions(WriteFlags.BufferHint);

        _readTask = Task.Run(HandleResponses, linked.Token);

        return;

        async Task HandleResponses() {
            Log.Information("Subscribing...");

            await foreach (var response in _call.ResponseStream.ReadAllAsync(cancellationToken: linked.Token)) {
                // add retries
                Log.Verbose("Received response: {Response}", response);
                await _handler(response, linked.Token);
            }
        }
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
            } catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable) {
                Log.Warning("Server unavailable");
            }

            _readTask.Dispose();
            _readTask = null;
        }

        _cts  = null;
        _call = null;
        Run(_ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async Task Project(ProjectionRequest projectionContext) {
        var retry = 0;

        while (!_disposing && !_ct.IsCancellationRequested) {
            var r = await ProjectInternal(projectionContext);

            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (r.Result) {
                case ProjectResult.Ok:
                    return;
                case ProjectResult.Retry:
                    _status = HealthCheckResult.Degraded(_host, r.Exception);
                    Log.Information("Retrying {Retry}", ++retry);
                    await Task.Delay(retry, _ct);

                    break;
                case ProjectResult.Fail:
                    _status = HealthCheckResult.Unhealthy(_host, r.Exception);
                    Log.Error("Projector to {Host} failed", _host);
                    retry++;
                    await Task.Delay(retry ^ 2, _ct);

                    break;
            }
        }
    }

    async Task<(ProjectResult Result, Exception? Exception)> ProjectInternal(ProjectionRequest projectionContext) {
        try {
            await _call!.RequestStream.WriteAsync(projectionContext, _ct);

            return (ProjectResult.Ok, null);
        } catch (InvalidOperationException e)
            when (e.Message.Contains("previous write is in progress")) {
            Log.Warning("Write already in progress");

            return (ProjectResult.Retry, e);
        } catch (ObjectDisposedException e) {
            await Resubscribe();

            return (ProjectResult.Retry, e);
        } catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable) {
            await Resubscribe();

            return (ProjectResult.Retry, e);
        } catch (Exception e) {
            return (ProjectResult.Fail, e);
        }
    }

    public HealthCheckResult GetStatus() => _status;

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
        } catch (OperationCanceledException) { } catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled) { }
    }
}

public enum ProjectResult {
    Ok,
    Retry,
    Fail
}
