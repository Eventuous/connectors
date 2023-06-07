// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Eventuous.Connector.Base.Diag;

public static class Logging {
    public static Logger GetLogger(
        IHostEnvironment                                    environment,
        LogEventLevel?                                      minimumLogLevel   = null,
        Func<LoggerSinkConfiguration, LoggerConfiguration>? sinkConfiguration = null,
        Func<LoggerConfiguration, LoggerConfiguration>?     configure         = null
    ) {
        var sc = sinkConfiguration ?? DefaultSink;

        var logLevel = minimumLogLevel
                    ?? (environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information);

        var logConfig = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel)
            // .MinimumLevel.Override("Eventuous", LogEventLevel.Verbose)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Grpc", LogEventLevel.Fatal)
            .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
            .Enrich.FromLogContext();

        logConfig = configure?.Invoke(logConfig) ?? logConfig;

        return sc(logConfig.WriteTo).CreateLogger();

        LoggerConfiguration DefaultSink(LoggerSinkConfiguration sinkConfig)
            => environment.IsDevelopment()
                ? sinkConfig.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>;{NewLine}{Exception}")
                : sinkConfig.Console(new RenderedCompactJsonFormatter());
    }

    public static void ConfigureSerilog(
        this WebApplicationBuilder                          builder,
        LogEventLevel?                                      minimumLogLevel   = null,
        Func<LoggerSinkConfiguration, LoggerConfiguration>? sinkConfiguration = null,
        Func<LoggerConfiguration, LoggerConfiguration>?     configure         = null
    ) {
        Log.CloseAndFlush();
        Log.Logger = GetLogger(builder.Environment, minimumLogLevel, sinkConfiguration, configure);
        builder.Host.UseSerilog();
    }
}
