using Microsoft.Extensions.Logging;
using Shared.Logging.Loggers;
using Shared.Logging.Models.Central;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Logging.Providers;

public sealed class CentralLoggerProvider(IPublishService publishService, Action<CentralLoggerConfiguration> configure) : ILoggerProvider
{
    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CentralLogger(categoryName, publishService, configure);
    }
}