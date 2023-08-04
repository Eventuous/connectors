// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector;
using Eventuous.Connector.Base.Diag;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

TypeMap.RegisterKnownEventTypes();

var tracingExporters = new ExporterMappings<TracerProviderBuilder>()
    .Add("otlp", b => b.AddOtlpExporter())
    .Add("zipkin", b => b.AddZipkinExporter())
    .Add("jaeger", b => b.AddJaegerExporter());

var metricsExporters = new ExporterMappings<MeterProviderBuilder>()
    .Add("prometheus", b => b.AddPrometheusExporter())
    .Add("otlp", b => b.AddOtlpExporter());

var app = new StartupBuilder("config.yaml", args).BuildApplication(tracingExporters, metricsExporters);

return await app.Run();
