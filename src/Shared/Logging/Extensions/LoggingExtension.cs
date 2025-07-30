using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shared.Logging.Models.Central;
using Shared.Logging.Models.ConsoleBeautify;
using Shared.Logging.Providers;
using Shared.Logging.Writer;

namespace Shared.Logging.Extensions;

public static class LoggingExtension
{
    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, IServiceCollection services, IConfiguration configuration)
    {
        var logConfig = new ConsoleBeautifyLoggerConfiguration();
        services.AddSingleton(logConfig);
        services.AddSingleton<ConsoleBeautifyChannelWriter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleBeautifyLoggerProvider>());
        // builder.AddProvider(new ConsoleBeautifyLoggerProvider(configuration));
        return builder;
    }

    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, IServiceCollection services, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        var logConfig = new ConsoleBeautifyLoggerConfiguration();
        configure(logConfig);
        logConfig.IsConfigured = true;
        services.AddSingleton(logConfig);
        services.AddSingleton<ConsoleBeautifyChannelWriter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleBeautifyLoggerProvider>());
        // builder.AddProvider(new ConsoleBeautifyLoggerProvider(null, configure));
        return builder;
    }

    public static ILoggingBuilder AddCentralLogger(this ILoggingBuilder builder, IServiceCollection services, Action<CentralLoggerConfiguration> configure)
    {
        var config = new CentralLoggerConfiguration();
        configure(config);
        services.AddSingleton(config);
        services.AddSingleton<CentralLogChannelWriter>();

        var centralLogChannelWriter = services.BuildServiceProvider().GetRequiredService<CentralLogChannelWriter>();
        builder.AddProvider(new CentralLoggerProvider(centralLogChannelWriter, config));

        // Alttaki kullanım circular dependency hatası oluşturur çünkü CentralLogChannelWriter içindeki servislerin bazıları ILogger bağımlı
        // services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, CentralLoggerProvider>());
        return builder;
    }
}