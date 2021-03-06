using System.Diagnostics;
using System.Text;
using Eventuous.Diagnostics;
using Eventuous.Subscriptions.Context;

namespace Eventuous.Connector.Base.Grpc; 

public class GrpcResponseHandler {

    readonly List<LocalContext> _contexts = new();

    public string Prepare(
        DelayedAckConsumeContext                   context,
        Func<DelayedAckConsumeContext, ValueTask>? next
    ) {
        var activity = context.Items.TryGetItem<Activity>("activity");
        _contexts.Add(new LocalContext(context, next, activity?.Context.TraceId, activity?.Context.SpanId));
        return Encoding.UTF8.GetString((context.Message as byte[])!);
    }

    public async Task Handler(ProjectionResponse result, CancellationToken cancellationToken) {
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
    
    record LocalContext(
        DelayedAckConsumeContext                   Context,
        Func<DelayedAckConsumeContext, ValueTask>? Next,
        ActivityTraceId?                           TraceId,
        ActivitySpanId?                            SpanId
    );
}
