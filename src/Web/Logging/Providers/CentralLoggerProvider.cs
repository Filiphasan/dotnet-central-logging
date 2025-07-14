using Web.Logging.Loggers;

namespace Web.Logging.Providers;

public sealed class CentralLoggerProvider : ILoggerProvider
{
    public void Dispose()
    {
        // no disposable object
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new CentralLogger();
    }
}