using Carter;
using Scalar.AspNetCore;
using Shared;
using Web.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logBuilder =>
{
    logBuilder.ClearProviders()
        // First way of usage
        // .AddConsoleBeautifyLogger(builder.Configuration)
        // Second way of usage
        .AddConsoleBeautifyLogger(cfg =>
        {
            cfg.JsonFormatEnabled = false;
            cfg.LogLevels["Default"] = LogLevel.Information;
            cfg.LogLevels["Microsoft"] = LogLevel.Warning;
            cfg.Enrichers["Application"] = builder.Environment.ApplicationName;
            cfg.Enrichers["Environment"] = builder.Environment.EnvironmentName;
        });
    // .AddCentralLogger(builder.Services, cfg =>
    // {
    //     cfg.IxdexPrefix = "web-api";
    //     cfg.LogLevels.TryAdd("Default", LogLevel.Information);
    //     cfg.LogLevels.TryAdd("Microsoft", LogLevel.Warning);
    // });
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddShared(builder.Configuration)
    .AddCarter();

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