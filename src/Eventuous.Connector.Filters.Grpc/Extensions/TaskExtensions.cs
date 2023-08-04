// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

namespace Eventuous.Connector.Filters.Grpc.Extensions;

static class TaskExtensions {
    public static async Task WhenAll(this IEnumerable<ValueTask> tasks) {
        var toAwait = tasks
            .Where(valueTask => !valueTask.IsCompletedSuccessfully)
            .Select(valueTask => valueTask.AsTask())
            .ToList();

        if (toAwait.Count > 0) await Task.WhenAll(toAwait).ConfigureAwait(false);
    }
}
