namespace Eventuous.Connector.EsdbMongo.Config;

public record EsdbConfig {
    public string ConnectionString { get; init; } = null!;
    public int    ConcurrencyLimit { get; init; } = 1;
}
