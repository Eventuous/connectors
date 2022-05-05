using Eventuous.Connector.Base.Tools;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Configuration;
using Serilog;

namespace Eventuous.Connector.Base.Grpc;

public sealed class Projector<TClient, TResponse> : IAsyncDisposable
    where TClient : ClientBase<TClient>, IProjectorClient<TResponse> {
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

    readonly TClient                                  _client;
    readonly Func<TResponse, CancellationToken, Task> _handler;

    Task?                    _readTask;
    CancellationTokenSource? _cts;
    CancellationToken        _ct;

    AsyncDuplexStreamingCall<ProjectionContext, TResponse>? _call;

    public Projector(string host, ChannelCredentials? credentials, Func<TResponse, CancellationToken, Task> handler) {
        _handler = handler;

        var channel = GrpcChannel.ForAddress(
            host,
            new GrpcChannelOptions {
                Credentials   = credentials,
                ServiceConfig = new ServiceConfig { MethodConfigs = { _defaultMethodConfig } }
            }
        );

        _client = CreateInstance(channel);
    }

    public void Run(CancellationToken cancellationToken) {
        _cts = new CancellationTokenSource();
        _ct  = cancellationToken;
        var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, _ct);

        _call = _client.Project(cancellationToken: linked.Token);

        async Task HandleResponses() {
            Log.Information("[Grpc] Subscribing...");

            await foreach (var response in _call.ResponseStream.ReadAllAsync(cancellationToken: linked.Token)) {
                // add retries
                Log.Verbose("[Grpc] Received response: {response}", response);
                await _handler(response, linked.Token);
            }
        }

        _readTask = Task.Run(HandleResponses, linked.Token);
    }

    async Task Resubscribe() {
        _cts?.Cancel();
        _call?.Dispose();

        if (_readTask != null) {
            try {
                await AwaitCancelled(() => _readTask);
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable) {
                Log.Warning("[Grpc] Server unavailable");
            }

            _readTask.Dispose();
            _readTask = null;
        }

        _cts  = null;
        _call = null;
        Run(_ct);
    }

    public async Task Project(ProjectionContext projectionContext) {
        var retry = 100;

        while (retry-- > 0) {
            var r = await ProjectInternal();

            if (r == ProjectResult.Ok) {
                break;
            }

            Log.Information($"[Grpc] Retrying {100 - retry}");
        }

        async Task<ProjectResult> ProjectInternal() {
            try {
                await _call!.RequestStream.WriteAsync(projectionContext);
                return ProjectResult.Ok;
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Unavailable) {
                await Resubscribe();
                return ProjectResult.Retry;
            }
            catch (Exception e) {
                Log.Error(e, "[Grpc] Projection failed");
                throw;
            }
        }
    }

    public async ValueTask DisposeAsync() {
        Log.Information("[Grpc] Disconnecting...");
        _cts?.Cancel();

        if (_call != null) {
            Log.Information("[Grpc] Closing the request stream...");
            await AwaitCancelled(() => _call.RequestStream.CompleteAsync());
        }

        if (_readTask != null) {
            Log.Information("[Grpc] Closing the reader...");
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

    static TClient CreateInstance(ChannelBase channel) {
        var constructors = typeof(TClient).GetConstructors<ChannelBase>();

        if (constructors.Length != 1) {
            throw new InvalidOperationException(
                $"{typeof(TClient).Name} must have a single constructor that takes a ChannelBase"
            );
        }

        var (ctor, _) = constructors[0];
        object[] args = { channel };

        if (ctor.Invoke(args) is not TClient instance)
            throw new InvalidOperationException($"Unable to instantiate {typeof(TClient)}");

        return instance;
    }
}

public enum ProjectResult {
    Ok,
    Retry
}
