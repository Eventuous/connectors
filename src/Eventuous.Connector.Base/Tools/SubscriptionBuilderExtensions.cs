using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Grpc;
using Eventuous.Subscriptions.Registrations;

namespace Eventuous.Connector.Base.Tools;

public static class SubscriptionBuilderExtensions {
    public static void AddGrpcProjector(this SubscriptionBuilder builder, GrpcProjectorSettings? settings)
        => builder.AddConsumeFilterLast(
            new GrpcProjectionFilter(settings.GetHost(), settings.GetCredentials())
        );
}
