namespace Eventuous.Connector.EsdbSqlServer.Config; 

public record EsdbConfig {
    public string ConnectionString { get; init; } = null!;
    public uint   ConcurrencyLimit { get; init; } = 1;
}
