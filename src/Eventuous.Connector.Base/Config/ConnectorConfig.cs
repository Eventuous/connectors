// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Eventuous.Connector.Base.Config;

public record ConnectorConfig<TSource, TTarget, TFilter> : ConnectorConfig
    where TSource : class
    where TTarget : class
    where TFilter : class {
    public TFilter? Filter { get; init; }
    public TSource  Source { get; init; } = null!;
    public TTarget  Target { get; init; } = null!;
}

public record ConnectorConfig {
    public ConnectorSettings Connector { get; init; } = new();
}

public record ConnectorSettings {
    public string            ConnectorId       { get; init; } = "default";
    public string            ConnectorAssembly { get; init; } = "";
    public string            ServiceName       { get; init; } = "eventuous-connector";
    public string            LogLevel          { get; init; } = "info";
    public DiagnosticsConfig Diagnostics       { get; init; } = new();
}

public record DiagnosticsConfig {
    public bool           Enabled                 { get; init; } = true;
    public TracingConfig? Tracing                 { get; [UsedImplicitly] init; }
    public MetricsConfig? Metrics                 { get; [UsedImplicitly] init; }
    public double         TraceSamplerProbability { get; init; } = 0;
}

public record MetricsConfig {
    public bool      Enabled   { get; init; } = true;
    public string[]? Exporters { get; [UsedImplicitly] init; }
}

public record TracingConfig {
    public bool      Enabled   { get; init; } = true;
    public string[]? Exporters { get; init; }
}
