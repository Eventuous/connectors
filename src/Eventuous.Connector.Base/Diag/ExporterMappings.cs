using Serilog;

namespace Eventuous.Connector.Base.Diag;

public class ExporterMappings<T> {
    readonly Dictionary<string, Action<T>> _mappings = new();

    public ExporterMappings<T> Add(string name, Action<T> configure) {
        _mappings.Add(name, configure);
        return this;
    }

    public void RegisterExporters(T provider, string[]? exporters) {
        var name = typeof(T).Name;
        if (exporters == null) {
            Log.Warning("No exporters for {name} available", name);
            return;
        }

        foreach (var exporter in exporters) {
            if (_mappings.TryGetValue(exporter, out var addExporter)) {
                Log.Information("Adding exporter {exporter} for {name}", exporter, name);
                addExporter(provider);
            }
            else {
                Log.Information("No exporters specified for {exporter}", exporter);
            }
        }
    }
}
