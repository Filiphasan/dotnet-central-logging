using Carter;
using Scalar.AspNetCore;
using Shared;
using Shared.Logging.Extensions;
using Web.Middlewares;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Logging.ClearProviders()
    .AddConsoleBeautifyLogger(builder.Services, builder.Configuration, options =>
    {
        options.AddEnricher("Application", builder.Environment.ApplicationName)
            .AddEnricher("Environment", builder.Environment.EnvironmentName)
            .SetMinimumLogLevel("Default", LogLevel.Information)
            .SetMinimumLogLevel("Microsoft", LogLevel.Warning);
    })
    .AddCentralLogger(builder.Services, options =>
    {
        options.SetLogKey("webapi")
            .SetExchangeName("central-logs-exchange")
            .AddEnricher("Application", builder.Environment.ApplicationName)
            .AddEnricher("Environment", builder.Environment.EnvironmentName)
            .SetMinimumLogLevel("Default", LogLevel.Information)
            .SetMinimumLogLevel("Microsoft", LogLevel.Warning);
    });

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddShared(builder.Configuration)
    .AddCarter();

builder.Services.AddTransient<CorrelationMiddleware>();
builder.Services.AddSingleton<RequestResponseLoggingMiddleware>();

var app = builder.Build();
app.UseMiddleware<CorrelationMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("doc/scalar", opt =>
    {
        opt.WithTitle("Logging")
            .WithDarkMode()
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.Shell, ScalarClient.Curl);
    });
}

app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseHttpsRedirection();

app.MapCarter();

await app.RunAsync();