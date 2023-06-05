// Copyright (C) 2021-2022 Ubiquitous AS. All rights reserved
// Licensed under the Apache License, Version 2.0.

using Microsoft.Extensions.FileProviders;

namespace Eventuous.Connector; 

public class StartupEnvironment : IHostEnvironment {
    public string        ApplicationName         { get; set; } = "Eventuous Connector";
    public IFileProvider ContentRootFileProvider { get; set; } = default!;
    public string        ContentRootPath         { get; set; } = default!;
    public string EnvironmentName { get; set; } = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                                               ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                                               ?? "Development";
}
