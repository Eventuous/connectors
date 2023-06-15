// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Eventuous.Connector.EsdbElastic.Config;

public record EsdbConfig {
    public string ConnectionString { get; init; } = null!;
    public int    ConcurrencyLimit { get; init; } = 1;
}
