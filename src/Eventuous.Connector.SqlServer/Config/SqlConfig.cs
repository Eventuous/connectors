namespace Eventuous.Connector.SqlServer.Config;

public record SqlConfig {
    public string ConnectionString { get; init; } = null!;
}
