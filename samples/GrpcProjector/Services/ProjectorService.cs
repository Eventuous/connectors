using Grpc.Core;

namespace GrpcProjector.Services;

public class ProjectorService : Projection.ProjectionBase {
    readonly ILogger<ProjectorService> _logger;

    public ProjectorService(ILogger<ProjectorService> logger) => _logger = logger;

    public override Task<GetCheckpointResponse> GetCheckpoint(GetCheckpointRequest request, ServerCallContext context) {
        _logger.LogInformation("Loading checkpoint");

        return Task.FromResult(new GetCheckpointResponse { Position = 0 });
    }

    public override Task<StoreCheckpointResponse> StoreCheckpoint(StoreCheckpointRequest request, ServerCallContext context) {
        _logger.LogInformation("Storing checkpoint {Position}", request.Position);

        return Task.FromResult(new StoreCheckpointResponse());
    }

    public override Task<ProjectResponse> Project(ProjectRequest request, ServerCallContext context) {
        foreach (var evt in request.Events) {
            _logger.LogInformation("Projecting event {Type} at {Stream}:{Position}", evt.EventType, evt.Stream, evt.EventNumber);
            Console.WriteLine(evt.EventPayload.ToString());
        }

        return Task.FromResult(new ProjectResponse());
    }
}
