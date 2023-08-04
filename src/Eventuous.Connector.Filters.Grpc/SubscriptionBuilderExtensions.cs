// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Eventuous.Connector.Filters.Grpc.Config;
using Eventuous.Subscriptions.Registrations;
using Microsoft.Extensions.DependencyInjection;

namespace Eventuous.Connector.Filters.Grpc;

public static class SubscriptionBuilderExtensions {
    public static void AddGrpcProjector(this SubscriptionBuilder builder, GrpcProjectorSettings settings, IHealthChecksBuilder healthChecks)
        => builder.AddConsumeFilterLast(new GrpcProjectionFilter(settings.GetHost(), settings.GetCredentials()));
}
