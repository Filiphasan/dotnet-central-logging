using Carter;
using Scalar.AspNetCore;
using Shared;
using Shared.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders()
    // .AddConsoleBeautifyLogger(builder.Configuration)
    .AddConsoleBeautifyLogger(cfg =>
    {
        cfg.JsonFormatEnabled = false;
        cfg.LogLevels["Default"] = LogLevel.Information;
        cfg.LogLevels["Microsoft"] = LogLevel.Warning;
        cfg.Enrichers["Application"] = builder.Environment.ApplicationName;
        cfg.Enrichers["Environment"] = builder.Environment.EnvironmentName;
    });

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddShared(builder.Configuration)
    .AddCarter();

builder.Logging.AddCentralLogger(builder.Services, cfg =>
{
    cfg.LogKey = "web-api";
    cfg.ExchangeName = "central-logs-exchange";
    cfg.LogLevels.TryAdd("Default", LogLevel.Information);
    cfg.LogLevels.TryAdd("Microsoft", LogLevel.Warning);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt.WithTitle("Logging")
            .WithDarkMode()
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl);
    });
}

app.UseHttpsRedirection();

app.MapCarter();

await app.RunAsync();