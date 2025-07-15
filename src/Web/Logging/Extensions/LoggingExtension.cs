using Web.Logging.Models.ConsoleBeautify;
using Web.Logging.Providers;

namespace Web.Logging.Extensions;

public static class LoggingExtension
{
    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, IConfiguration configuration)
    {
        builder.AddProvider(new ConsoleBeautifyLoggerProvider(configuration));
        return builder;
    }

    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        builder.AddProvider(new ConsoleBeautifyLoggerProvider(configure: configure));
        return builder;
    }

    public static ILoggingBuilder AddCentralLogger(this ILoggingBuilder builder)
    {
        builder.AddProvider(new CentralLoggerProvider());
        return builder;
    }
}