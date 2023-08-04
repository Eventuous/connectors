using GrpcProjector.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc.Infrastructure", LogEventLevel.Warning)
    .MinimumLevel.Override("Grpc", LogEventLevel.Information)
    .MinimumLevel.Override("Grpc.Net.Client.Internal.GrpcCall", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>;{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddGrpc();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1",
        new OpenApiInfo { Title = "gRPC transcoding", Version = "v1" });
});

if (builder.Environment.IsDevelopment()) {
    builder.WebHost.ConfigureKestrel(options => options.ListenLocalhost(9091, o => o.Protocols = HttpProtocols.Http1AndHttp2));
}

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
});
app.MapGrpcService<ProjectorService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client");

try {
    var port = Environment.GetEnvironmentVariable("PORT");
    var url  = string.IsNullOrWhiteSpace(port) ? null : $"http://+:{port}";
    app.Run(url);

    return 0;
} catch (Exception e) {
    Log.Fatal(e, "Host terminated unexpectedly");

    return 1;
} finally { Log.CloseAndFlush(); }
