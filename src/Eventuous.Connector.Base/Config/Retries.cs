// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace Eventuous.Connector.Base.Config;

public delegate IAsyncPolicy ResolveRetryPolicy(IServiceProvider serviceProvider);

public static class RetryPolicies {
    public static IAsyncPolicy NoRetries(IServiceProvider _) => Policy.NoOpAsync();

    public static IAsyncPolicy RetryForever<T>(IServiceProvider sp, ConnectorConfig config) where T : Exception {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var log           = loggerFactory.CreateLogger(config.Connector.ConnectorId);

        return Policy
            .Handle<T>()
            .WaitAndRetryForeverAsync(
                (retryAttempt, _) => TimeSpan.FromMilliseconds(retryAttempt * 100),
                LogRetry
            );

        void LogRetry(Exception exception, int retry, TimeSpan delay, Context context)
            => log.LogWarning("Retrying after {RetryAttempt} attempt(s), waiting for {Delay}", retry, delay);
    }
}
