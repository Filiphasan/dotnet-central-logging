using Web.Logging.Loggers;
using Web.Logging.Models;
using Web.Services.Interfaces;

namespace Web.Logging.Providers;

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