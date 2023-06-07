using GrpcProjector.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

if (builder.Environment.IsDevelopment()) {
    builder.WebHost.ConfigureKestrel(
        options => options.ListenLocalhost(9091, o => o.Protocols = HttpProtocols.Http2)
    );
}

var app = builder.Build();
app.MapGrpcService<ProjectorService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client");
var port = Environment.GetEnvironmentVariable("PORT");
var url  = string.IsNullOrWhiteSpace(port) ? null : $"http://+:{port}";
app.Run(url);
