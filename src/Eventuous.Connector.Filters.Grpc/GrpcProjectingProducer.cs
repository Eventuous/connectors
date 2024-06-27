// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using System.Runtime.CompilerServices;
using Eventuous.Producers;
using Eventuous.Producers.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace Eventuous.Connector.Filters.Grpc;

public abstract class GrpcProjectingProducer<T, TOptions> : BaseProducer<TOptions>
    where T : class, IProducer<TOptions>
    where TOptions : class {
    protected GrpcProjectingProducer(ProducerTracingOptions? tracingOptions = null) : base(tracingOptions) { }

    readonly Dictionary<string, Func<ProjectionResponse, StreamName, CancellationToken, Task>>
        _projectorsByName = new();

    protected void On<TEvent>(Func<ProjectedMessage<TEvent>, CancellationToken, Task> projector)
        where TEvent : class, IMessage<TEvent>, new() {
        var temp = new TEvent();
        _projectorsByName.Add(temp.Descriptor.FullName, ProjectAny);

        return;

        static TEvent GetEvent(Any message) {
            var evt = new TEvent();
            evt.MergeFrom(message.Value);
            return evt;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Task ProjectAny(ProjectionResponse responseContext, StreamName streamName, CancellationToken token) {
            var evt     = GetEvent(responseContext.Operation);
            var message = new ProjectedMessage<TEvent>(evt, streamName, responseContext.Metadata);
            return projector(message, token);
        }
    }

    protected override Task ProduceMessages(
        StreamName                   stream,
        IEnumerable<ProducedMessage> messages,
        TOptions?                    options,
        CancellationToken            cancellationToken = default
    ) {
        var tasks = messages.Select(x => Project(x, stream, cancellationToken));
        return Task.WhenAll(tasks);
    }

    async Task Project(ProducedMessage message, StreamName streamName, CancellationToken cancellationToken) {
        if (message.Message is not ProjectionResponse response || response.Operation.Is(Ignore.Descriptor)) {
            return;
        }

        var typeName = Any.GetTypeName(response.Operation.TypeUrl);

        if (!_projectorsByName.TryGetValue(typeName, out var projector)) {
            throw new InvalidOperationException($"No projector found for type: {typeName}");
        }

        try {
            await projector(response, streamName, cancellationToken).ConfigureAwait(false);
            await message.Ack<T>().ConfigureAwait(false);
        }
        catch (Exception e) {
            await message.Nack<T>(e.Message, e).ConfigureAwait(false);
        }
    }
}

public record ProjectedMessage<T>(T Message, StreamName Stream, MapField<string, string> Metadata);
