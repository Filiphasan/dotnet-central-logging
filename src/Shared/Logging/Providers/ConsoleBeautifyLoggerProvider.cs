using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Logging.Loggers;
using Shared.Logging.Models.ConsoleBeautify;
using Shared.Logging.Writer;

namespace Shared.Logging.Providers;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleBeautify")]
public sealed class ConsoleBeautifyLoggerProvider(IConfiguration configuration, ConsoleBeautifyLoggerConfiguration config, ConsoleBeautifyChannelWriter consoleBeautifyChannelWriter)
    : ILoggerProvider
{
    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return !config.IsConfigured
            ? new ConsoleBeautifyLogger(categoryName, configuration, consoleBeautifyChannelWriter)
            : new ConsoleBeautifyLogger(categoryName, config, consoleBeautifyChannelWriter);
    }
}