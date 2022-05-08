namespace Eventuous.Connector.EsdbMongo.Config;

public record MongoConfig {
    public string? ConnectionString { get; init; }
    public string? Database         { get; init; }
    public string? Collection       { get; init; }
    public string  ConnectorMode    { get; init; } = "project";
}
