// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Eventuous.Connector.Base.App;

public static class Hosting {
    public static async Task RunConnector(this WebApplication host) {
        var jobs = host.Services.GetServices<IStartupJob>().ToArray();

        if (jobs.Length > 0) {
            await Task.WhenAll(jobs.Select(x => x.Run()));
        }

        await host.RunAsync();
    }
}
