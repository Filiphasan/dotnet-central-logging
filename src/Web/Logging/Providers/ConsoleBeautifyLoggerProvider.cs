using System.Runtime.Versioning;
using Web.Logging.Loggers;

namespace Web.Logging.Providers;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("ConsoleBeautify")]
public sealed class ConsoleBeautifyLoggerProvider(IConfiguration configuration) : ILoggerProvider
{
    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleBeautifyLogger(categoryName, configuration);
    }
}