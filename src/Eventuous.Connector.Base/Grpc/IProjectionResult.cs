namespace Eventuous.Connector.Base.Grpc; 

public interface IProjectionResult {
    string EventId { get; }
}
