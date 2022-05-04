using Grpc.Core;
using grpc = Grpc.Core;

namespace Eventuous.Connector.Base.Grpc;

public interface IProjectorClient<TResponse> {
    AsyncDuplexStreamingCall<ProjectionContext, TResponse> Project(
        grpc::Metadata?   headers           = null,
        DateTime?         deadline          = null,
        CancellationToken cancellationToken = default
    );
}
