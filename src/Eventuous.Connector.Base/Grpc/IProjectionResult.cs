namespace Eventuous.Connector.Base.Grpc; 

public interface IProjectionResult {
    ResponseContext Context { get; }
}