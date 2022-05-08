using Eventuous.Gateway;
using Eventuous.Subscriptions.Context;

namespace Eventuous.Connector.Base.Grpc;

public class GrpcTransform<TProduceOptions, TResult> : IGatewayTransform<TProduceOptions>
    where TProduceOptions : new() where TResult : IProjectionResult {
    readonly string _target;

    public GrpcTransform(string target) => _target = target;

    static readonly TProduceOptions Options = new();

    public ValueTask<GatewayMessage<TProduceOptions>[]> RouteAndTransform(IMessageConsumeContext context) {
        var projectionResult = context.Items.TryGetItem<TResult>("projectionResult");

        var gatewayMessage = new GatewayMessage<TProduceOptions>(
            new StreamName(_target),
            projectionResult!,
            null,
            Options
        );

        return new ValueTask<GatewayMessage<TProduceOptions>[]>(new[] { gatewayMessage });
    }
}
