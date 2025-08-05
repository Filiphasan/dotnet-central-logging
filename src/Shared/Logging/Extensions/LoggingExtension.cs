using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Models.Central;
using Shared.Logging.Models.ConsoleBeautify;
using Shared.Logging.Models.FileLog;
using Shared.Logging.Providers;
using Shared.Logging.Writer;

namespace Shared.Logging.Extensions;

public static class LoggingExtension
{
    public static ILoggingBuilder AddConsoleBeautifyLogger(this ILoggingBuilder builder, IServiceCollection services, IConfiguration configuration, Action<ConsoleBeautifyLoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ConsoleBeautifyLoggerConfiguration();
        configure(options);

        LoggerHelper.LoadConsoleBeautifyOptionsFromConfiguration(configuration, options);

        var writerOptions = new ConsoleBeautifyChannelWriterConfiguration
        {
            JsonFormatEnabled = options.JsonFormatEnabled,
            LogLevelColors = options.LogLevelColors,
            ChannelBound = options.ChannelBound,
        };

        services.AddSingleton(options);
        services.AddSingleton(writerOptions);
        services.AddSingleton<ConsoleBeautifyChannelWriter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, ConsoleBeautifyLoggerProvider>());
        return builder;
    }

    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, IServiceCollection services, Action<FileLoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new FileLoggerConfiguration();
        configure(options);

        var writerOptions = new FileLogChannelWriterConfiguration
        {
            MaxParallelism = options.MaxParallelism,
            WriteSize = options.WriteSize,
            WriteInterval = options.WriteInterval,
            BaseFolder = options.BaseFolder,
        };

        services.AddSingleton(options);
        services.AddSingleton(writerOptions);
        services.TryAddSingleton(new ConsoleBeautifyChannelWriterConfiguration());
        services.TryAddSingleton<ConsoleBeautifyChannelWriter>();
        services.AddSingleton<FileLogChannelWriter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
        return builder;
    }

    public static ILoggingBuilder AddCentralLogger(this ILoggingBuilder builder, IServiceCollection services, Action<CentralLoggerConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CentralLoggerConfiguration();
        configure(options);

        if (options.LogKey.Contains('.'))
        {
            throw new ArgumentException("LogKey cannot contain '.'");
        }

        var writerOptions = new CentralLogChannelWriterConfiguration
        {
            ExchangeName = options.ExchangeName,
            IsSpecific = options.IsSpecific,
            ChannelBound = options.ChannelBound,
            MaxParallelizm = options.MaxParallelizm,
        };
        services.AddSingleton(options);
        services.AddSingleton(writerOptions);
        services.TryAddSingleton(new ConsoleBeautifyChannelWriterConfiguration());
        services.TryAddSingleton<ConsoleBeautifyChannelWriter>();
        services.AddSingleton<CentralLogChannelWriter>();

        // Alttaki kullanım normalde circular dependency hatası oluşturur çünkü
        // CentralLogChannelWriter içindeki servislerin bazıları ILogger bağımlı bu sorunun çözümü için CentralLogChannelWriter ctor içinde özel bir if eklendi
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, CentralLoggerProvider>());

        // CentralLogChannelWriter içindeki özel çözüm kullanılmak istenmezse bu şekilde kullanılabilir
        // Ama bu da kendine ait bir CentralLogChannelWriter oluşturur (Genel DI konteynere ait olmayan bir CentralLogChannelWriter)
        // var centralLogChannelWriter = services.BuildServiceProvider().GetRequiredService<CentralLogChannelWriter>();
        // builder.AddProvider(new CentralLoggerProvider(centralLogChannelWriter, config));
        return builder;
    }
}