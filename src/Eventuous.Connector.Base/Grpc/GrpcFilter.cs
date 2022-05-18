using Eventuous.Subscriptions.Context;
using Eventuous.Subscriptions.Filters;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Eventuous.Connector.Base.Grpc;

public sealed class GrpcProjectionFilter : ConsumeFilter<DelayedAckConsumeContext>, IAsyncDisposable {
    readonly Projector[]           _projectors;
    readonly GrpcResponseHandler[] _responseHandlers;

    public GrpcProjectionFilter(string host, ChannelCredentials credentials, int partitionCount) {
        _projectors       = new Projector[partitionCount];
        _responseHandlers = new GrpcResponseHandler[partitionCount];

        for (var i = 0; i < partitionCount; i++) {
            var responseHandler = new GrpcResponseHandler();
            var projector       = new Projector(host, credentials, responseHandler.Handler);
            projector.Run(default);
            _projectors[i]       = projector;
            _responseHandlers[i] = responseHandler;
        }
    }

    public override async ValueTask Send(
        DelayedAckConsumeContext                   context,
        Func<DelayedAckConsumeContext, ValueTask>? next
    ) {
        var json = _responseHandlers[context.PartitionId].Prepare(context, next);

        await _projectors[context.PartitionId]
            .Project(
                new ProjectionRequest {
                    Stream       = context.Stream,
                    EventType    = context.MessageType,
                    EventId      = context.MessageId,
                    EventPayload = Struct.Parser.ParseJson(json)
                }
            );
    }

    public async ValueTask DisposeAsync() => await _projectors.Select(x => x.DisposeAsync()).WhenAll();
}
