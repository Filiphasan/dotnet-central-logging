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
    .AddConsoleBeautifyLogger(options =>
    {
        options
            .AddEnricher("Application", builder.Environment.ApplicationName)
            .AddEnricher("Environment", builder.Environment.EnvironmentName)
            .SetMinimumLogLevel("Default", LogLevel.Information)
            .SetMinimumLogLevel("Microsoft", LogLevel.Warning);
    });

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddShared(builder.Configuration)
    .AddCarter();

builder.Logging.AddCentralLogger(builder.Services, options =>
{
    options.SetLogKey("web-api")
        .SetExchangeName("central-logs-exchange")
        .AddEnricher("Application", builder.Environment.ApplicationName)
        .AddEnricher("Environment", builder.Environment.EnvironmentName)
        .SetMinimumLogLevel("Default", LogLevel.Information)
        .SetMinimumLogLevel("Microsoft", LogLevel.Warning);
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