using Carter;
using Scalar.AspNetCore;
using Shared;
using Web.Logging.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(logBuilder =>
{
    logBuilder.ClearProviders()
        .AddConsoleBeautifyLogger(builder.Configuration)
        .AddCentralLogger();
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