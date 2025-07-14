using Shared;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddShared(builder.Configuration);

var host = builder.Build();
await host.RunAsync();