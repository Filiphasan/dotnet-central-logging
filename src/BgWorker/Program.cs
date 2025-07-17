using Shared;
using Shared.Logging.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging
    .ClearProviders()
    .AddConsoleBeautifyLogger(builder.Configuration);

builder.Services.AddShared(builder.Configuration);

var host = builder.Build();
await host.RunAsync();