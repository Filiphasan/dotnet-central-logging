using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Logging.Loggers;
using Shared.Logging.Models.ConsoleBeautify;

namespace Shared.Logging.Providers;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleBeautify")]
public sealed class ConsoleBeautifyLoggerProvider(IConfiguration? configuration = null, Action<ConsoleBeautifyLoggerConfiguration>? configure = null)
    : ILoggerProvider
{
    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (configuration is null && configure is null)
        {
            throw new InvalidOperationException("Either configuration or configure must be provided.");
        }

        return configure is null
            ? new ConsoleBeautifyLogger(categoryName, configuration!)
            : new ConsoleBeautifyLogger(categoryName, configure);
    }
}