using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using Shared.Logging.Loggers;
using Shared.Logging.Models.Central;
using Shared.Logging.Writer;

namespace Shared.Logging.Providers;

[UnsupportedOSPlatform("browser")]
[ProviderAlias("CentralLogger")]
public sealed class CentralLoggerProvider(CentralLogChannelWriter centralLogChannelWriter, CentralLoggerConfiguration config) : ILoggerProvider
{
    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CentralLogger(categoryName, centralLogChannelWriter, config);
    }
}