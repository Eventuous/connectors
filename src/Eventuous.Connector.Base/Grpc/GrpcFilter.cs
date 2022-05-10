using System.Diagnostics;
using System.Text;
using Eventuous.Diagnostics;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Eventuous.Connector.Base.Grpc;

public sealed class GrpcProjectionFilter : ConsumeFilter<DelayedAckConsumeContext>, IAsyncDisposable {
    readonly Projector _projector;

    public GrpcProjectionFilter(string host) {
        _projector = new Projector(host, ChannelCredentials.Insecure, Handler);
        _projector.Run(default);
    }

    async Task Handler(ProjectionResponse result, CancellationToken cancellationToken) {
        var ctx = _contexts.Single(x => x.Context.MessageId == result.EventId);

        using var activity = Start();
        _contexts.Remove(ctx);
        await ctx.Next!(ctx.Context.WithItem("projectionResult", result));

        Activity? Start()
            => ctx.TraceId == null || ctx.SpanId == null ? null
                : EventuousDiagnostics.ActivitySource.StartActivity(
                    ActivityKind.Producer,
                    new ActivityContext(
                        ctx.TraceId.Value,
                        ctx.SpanId.Value,
                        ActivityTraceFlags.Recorded
                    )
                );
    }

    public override async ValueTask Send(
        DelayedAckConsumeContext                   context,
        Func<DelayedAckConsumeContext, ValueTask>? next
    ) {
        var activity = context.Items.TryGetItem<Activity>("activity");
        _contexts.Add(new LocalContext(context, next, activity?.Context.TraceId, activity?.Context.SpanId));
        var json = Encoding.UTF8.GetString((context.Message as byte[])!);

        await _projector.Project(
            new ProjectionRequest {
                Stream       = context.Stream,
                EventType    = context.MessageType,
                EventId      = context.MessageId,
                EventPayload = Struct.Parser.ParseJson(json)
            }
        );
    }

    public ValueTask DisposeAsync() => _projector.DisposeAsync();

    record LocalContext(
        DelayedAckConsumeContext                   Context,
        Func<DelayedAckConsumeContext, ValueTask>? Next,
        ActivityTraceId?                           TraceId,
        ActivitySpanId?                            SpanId
    );

    readonly List<LocalContext> _contexts = new();
}
