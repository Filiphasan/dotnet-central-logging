using BgWorker;
using Shared.Logging.Extensions;

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging
    .ClearProviders()
    .AddConsoleBeautifyLogger(builder.Services, builder.Configuration, options =>
    {
        options.AddEnricher("Environment", builder.Environment.EnvironmentName);
    });

builder.Services.AddBgWorker(builder.Configuration);

var host = builder.Build();
await host.RunAsync();