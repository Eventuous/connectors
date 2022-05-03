namespace Eventuous.Connector.EsdbElastic.Conversions; 

public class ElasticMetadata {
    public static Dictionary<string, string?>? FromMetadata(Metadata? metadata)
        => metadata?.ToDictionary(x => x.Key, x => x.Value?.ToString());
}
