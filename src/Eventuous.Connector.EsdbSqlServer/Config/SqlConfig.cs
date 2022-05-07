namespace Eventuous.Connector.EsdbSqlServer.Config;

public record SqlConfig {
    public string ConnectionString { get; init; } = null!;
}
