using BgWorker.Models;
using Shared.Logging.Models;

namespace BgWorker.Helpers;

public static class EcsLogEntryHelper
{
    public static EcsLogEntryModel LogEntryToEcs(LogEntryModel logEntry)
    {
        return new EcsLogEntryModel
        {
            Timestamp = logEntry.Timestamp,
            Message = logEntry.Message,
            Log = new EcsLog
            {
                Level = logEntry.Level,
                Logger = logEntry.Source
            },
            Event = new EcsEvent
            {
                Id = logEntry.EventId?.ToString(),
                Action = logEntry.EventName,
                Reference = logEntry.LogKey
            },
            Error = logEntry.Exception?.ToEcsError(),
            Enrichers = logEntry.Enrichers,
            Properties = logEntry.Properties
        };
    }

    private static EcsError? ToEcsError(this ExceptionDetailModel? ex)
    {
        if (ex is null)
            return null;

        // InnerException'ları da stack trace'e dahil etmek daha faydalıdır.
        var fullStackTrace = ex.StackTrace;
        var currentInner = ex.InnerException;
        while(currentInner != null)
        {
            fullStackTrace += "\n--- Inner Exception ---\n" +
                              $"Type: {currentInner.Type}\n" +
                              $"Message: {currentInner.Message}\n" +
                              $"StackTrace: {currentInner.StackTrace}";
            currentInner = currentInner.InnerException;
        }

        return new EcsError
        {
            Type = ex.Type,
            Message = ex.Message,
            StackTrace = fullStackTrace
        };
    }
}