using Eventuous.Connector.Base.Grpc;

// ReSharper disable CheckNamespace

namespace Eventuous.Connector.Mongo;

public static partial class Projection {
    public partial class ProjectionClient : IProjectorClient<ProjectionResult> { }
}

public partial class ProjectionResult : IProjectionResult { }
