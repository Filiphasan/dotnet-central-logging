using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Logging.Models.Central;
using Shared.Logging.Models.ConsoleBeautify;
using Shared.Logging.Providers;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Logging.Extensions;

public static class LoggingExtension
{
    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.AddProvider(new ConsoleBeautifyLoggerProvider(configuration));
        return builder;
    }

    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        builder.AddProvider(new ConsoleBeautifyLoggerProvider(null, configure));
        return builder;
    }

    public static ILoggingBuilder AddCentralLogger(this ILoggingBuilder builder, IServiceCollection services, Action<CentralLoggerConfiguration> configure)
    {
        var publishService = services.BuildServiceProvider().GetRequiredService<IPublishService>();
        builder.AddProvider(new CentralLoggerProvider(publishService, configure));
        return builder;
    }
}