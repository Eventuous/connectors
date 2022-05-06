using System.Reflection;
using Eventuous.AspNetCore;
using Eventuous.Connector.Base;
using Eventuous.Connector.Base.App;
using Eventuous.Connector.Base.Config;
using Eventuous.Connector.Base.Diag;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;

namespace Eventuous.Connector;

public class StartupBuilder {
    ConnectorConfig? _config;
    ConnectorApp?    _app;
    string?          _configFile;

    readonly Logger _log;

    public StartupBuilder() {
        var logConfig = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console();
        _log = logConfig.CreateLogger();
    }

    public StartupBuilder Configure(string configFile, string[] args) {
        _log.Information("Configuring connector using config file {configFile}", configFile);

        _configFile = configFile;
        var hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.ConfigureHostConfiguration(c => c.AddYamlFile(configFile));
        using var tempHost = hostBuilder.Build();
        _config = tempHost.Services.GetService<IConfiguration>().Get<ConnectorConfig>();

        if (string.IsNullOrWhiteSpace(_config.Connector.ConnectorAssembly)) {
            _log.Fatal($"Connector assembly must be specified in {configFile}");
            throw new ApplicationException();
        }

        return this;
    }

    public StartupBuilder BuildApplication(
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        if (_config == null) {
            _log.Fatal("Call Configure() first");
            throw new ApplicationException();
        }

        var location = Assembly.GetExecutingAssembly().Location;
        var path     = Path.GetDirectoryName(location);

        var assemblyFileName = _config.Connector.ConnectorAssembly.EndsWith("dll")
            ? _config.Connector.ConnectorAssembly
            : _config.Connector.ConnectorAssembly + ".dll";

        _log.Information("Loading connector assembly {assemblyFileName}", assemblyFileName);
        
        var assembly = Assembly.LoadFrom(Path.Join(path, assemblyFileName));
        var startup  = assembly.GetTypes().FirstOrDefault(x => x.IsAssignableTo(typeof(IConnectorStartup)));
        
        if (startup == null) {
            _log.Fatal("Connector assembly must have an implementation of IConnectorStartup");
            throw new ApplicationException();
        }
        
        var startupInstance = Activator.CreateInstance(startup) as IConnectorStartup;

        _log.Information("Building connector application");

        _app = startupInstance!.BuildConnectorApp(_configFile!, tracingExporters, metricsExporters);

        return this;
    }

    public Task<int> Run() {
        if (_config == null) {
            _log.Fatal("Call Configure() first");
            throw new ApplicationException();
        }

        if (_app == null) {
            _log.Fatal("Call BuildApplication() first");
            throw new ApplicationException();
        }

        if (_config.Connector.Diagnostics.Enabled && _config.Connector.Diagnostics.Metrics?.Enabled == true
         && _config.Connector.Diagnostics.Metrics.Exporters?.Any(x => x == "prometheus")            == true) {
            _log.Information("Adding Prometheus metrics exporter");
            _app.Host.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        _app.Host.AddEventuousLogs();

        _log.Information("Starting connector application");
        return _app.Run();
    }
}
