using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Shared.Logging.Loggers;
using Shared.Logging.Models.Central;
using Shared.Logging.Writer;

namespace Shared.Logging.Providers;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("CentralLog")]
public sealed class CentralLoggerProvider(CentralLogChannelWriter centralLogChannelWriter, CentralLoggerConfiguration config) : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, CentralLogger> _loggers = new();

    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new CentralLogger(name, centralLogChannelWriter, config));
    }
}