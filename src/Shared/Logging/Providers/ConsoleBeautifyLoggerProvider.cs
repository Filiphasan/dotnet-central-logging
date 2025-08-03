using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Shared.Logging.Loggers;
using Shared.Logging.Models.ConsoleBeautify;
using Shared.Logging.Writer;

namespace Shared.Logging.Providers;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleBeautify")]
public sealed class ConsoleBeautifyLoggerProvider(ConsoleBeautifyLoggerConfiguration options, ConsoleBeautifyChannelWriter consoleBeautifyChannelWriter)
    : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, ConsoleBeautifyLogger> _loggers = new();

    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new ConsoleBeautifyLogger(name, options, consoleBeautifyChannelWriter));
    }
}