// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Diagnostics;
using System.Text;
using Eventuous.Diagnostics;
using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;

namespace Eventuous.Connector.Base.Grpc;

public class GrpcResponseHandler {
    readonly List<LocalContext> _contexts = new();

    public string Prepare(AsyncConsumeContext context, LinkedListNode<IConsumeFilter>? next) {
        context.Items.TryGetItem<Activity>(ContextItemKeys.Activity, out var activity);
        _contexts.Add(new LocalContext(context, next, activity?.Context.TraceId, activity?.Context.SpanId));
        return Encoding.UTF8.GetString((context.Message as byte[])!);
    }

    public async Task Handler(ProjectionResponse result, CancellationToken cancellationToken) {
        var ctx = _contexts.Single(x => x.Context.MessageId == result.EventId);

        using var activity = Start();
        _contexts.Remove(ctx);
        if (ctx.Next is null) return;
        await ctx.Next.Value.Send(ctx.Context.WithItem(GrpcContextKeys.ProjectionResult, result), ctx.Next.Next);

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
        AsyncConsumeContext             Context,
        LinkedListNode<IConsumeFilter>? Next,
        ActivityTraceId?                TraceId,
        ActivitySpanId?                 SpanId
    );
}
