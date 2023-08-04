// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.DependencyInjection;

namespace Eventuous.Connector.Base.App;

public interface IStartupJob {
    Task Run();
}

public class StartupJob<T1, T2>(T1 t1, T2 t2, Func<T1, T2, Task> func) : IStartupJob {
    public Task Run() => func(t1, t2);
}

public static class StartupJobRegistration {
    public static void AddStartupJob<T1, T2>(this IServiceCollection services, Func<T1, T2, Task> func)
        where T1 : class where T2 : class
        => services.AddSingleton(func).AddSingleton<IStartupJob, StartupJob<T1, T2>>();
}
