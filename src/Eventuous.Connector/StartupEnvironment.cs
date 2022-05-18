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
