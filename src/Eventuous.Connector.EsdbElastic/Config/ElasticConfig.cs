using Eventuous.ElasticSearch.Index;

namespace Eventuous.Connector.EsdbElastic.Config;

public record ElasticConfig {
    public string?      ConnectionString { get; init; }
    public string?      CloudId          { get; init; }
    public string?      ApiKey           { get; init; }
    public IndexConfig? DataStream       { get; init; }
}
