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
using ILogger = Serilog.ILogger;

namespace Eventuous.Connector;

sealed class StartupBuilder {
    readonly ConnectorConfig? _config;
    ConnectorApp?             _app;
    readonly string?          _configFile;

    static readonly ILogger Log = Serilog.Log.ForContext<StartupBuilder>();

    public StartupBuilder(string configFile, string[] args) {
        Serilog.Log.Logger = Logging.GetLogger(new StartupEnvironment());
        Log.Information("Configuring connector using config file {configFile}", configFile);
        
        _configFile = configFile;
        var hostBuilder = Host.CreateDefaultBuilder(args);
        hostBuilder.ConfigureHostConfiguration(c => c.AddYamlFile(configFile));
        using var tempHost = hostBuilder.Build();
        _config = tempHost.Services.GetService<IConfiguration>().Get<ConnectorConfig>();

        if (string.IsNullOrWhiteSpace(_config.Connector.ConnectorAssembly)) {
            Log.Fatal($"Connector assembly must be specified in {configFile}");
            throw new ApplicationException();
        }
    }

    public StartupBuilder BuildApplication(
        ExporterMappings<TracerProviderBuilder> tracingExporters,
        ExporterMappings<MeterProviderBuilder>  metricsExporters
    ) {
        if (_config == null) {
            Log.Fatal("Call Configure() first");
            throw new ApplicationException();
        }

        var location = Assembly.GetExecutingAssembly().Location;
        var path     = Path.GetDirectoryName(location);

        var assemblyFileName = _config.Connector.ConnectorAssembly.EndsWith("dll")
            ? _config.Connector.ConnectorAssembly
            : _config.Connector.ConnectorAssembly + ".dll";

        Log.Information("Loading connector assembly {assemblyFileName}", assemblyFileName);

        var assembly = Assembly.LoadFrom(Path.Join(path, assemblyFileName));
        var startup  = assembly.GetTypes().FirstOrDefault(x => x.IsAssignableTo(typeof(IConnectorStartup)));

        if (startup == null) {
            Log.Fatal("Connector assembly must have an implementation of IConnectorStartup");
            throw new ApplicationException();
        }

        var startupInstance = Activator.CreateInstance(startup) as IConnectorStartup;

        Log.Information("Building connector application");

        _app = startupInstance!.BuildConnectorApp(_configFile!, tracingExporters, metricsExporters);

        return this;
    }

    public async Task<int> Run() {
        if (_config == null) {
            Log.Fatal("Call Configure() first");
            throw new ApplicationException();
        }

        if (_app == null) {
            Log.Fatal("Call BuildApplication() first");
            throw new ApplicationException();
        }

        if (_config.Connector.Diagnostics.Enabled && _config.Connector.Diagnostics.Metrics?.Enabled == true
                                                  && _config.Connector.Diagnostics.Metrics.Exporters?.Any(
                                                         x => x == "prometheus"
                                                     ) == true) {
            Log.Information("Adding Prometheus metrics exporter");
            _app.Host.UseOpenTelemetryPrometheusScrapingEndpoint();
        }

        _app.Host.AddEventuousLogs();
        _app.Host.MapGet("ping", ctx => ctx.Response.WriteAsync("pong"));
        _app.Host.MapHealthChecks("/health");

        Log.Information("Starting connector application");

        try {
            return await _app.Run();
        }
        catch (Exception e) {
            Log.Fatal(e, "Connector application failed");
            Serilog.Log.CloseAndFlush();
            return -1;
        }
    }
}
