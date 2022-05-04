using System.Text;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Grpc.Core;

namespace Eventuous.Connector.Base.Grpc;

public sealed class GrpcProjectionFilter<TClient, TResult> : ConsumeFilter<DelayedAckConsumeContext>, IAsyncDisposable
    where TClient : ClientBase<TClient>, IProjectorClient<TResult>
    where TResult : IProjectionResult {
    readonly Projector<TClient, TResult> _projector;

    public GrpcProjectionFilter(string host) {
        _projector =
            new Projector<TClient, TResult>(host, ChannelCredentials.Insecure, Handler);

        _projector.Run(default);
    }

    async Task Handler(TResult result, CancellationToken cancellationToken) {
        var messageContext = _contexts.Single(x => x.Context.MessageId == result.EventId);
        _contexts.Remove(messageContext);
        await messageContext.Next!(messageContext.Context.WithItem("projectionResult", result));
    }

    public override async ValueTask Send(
        DelayedAckConsumeContext                   context,
        Func<DelayedAckConsumeContext, ValueTask>? next
    ) {
        _contexts.Add(new LocalContext(context, next));
        var json = Encoding.UTF8.GetString((context.Message as byte[])!);

        await _projector.Project(
            new ProjectionContext {
                Stream    = context.Stream,
                EventType = context.MessageType,
                EventId   = context.MessageId,
                EventJson = json
            }
        );
    }

    public ValueTask DisposeAsync() => _projector.DisposeAsync();

    record LocalContext(DelayedAckConsumeContext Context, Func<DelayedAckConsumeContext, ValueTask>? Next);

    readonly List<LocalContext> _contexts = new();
}
