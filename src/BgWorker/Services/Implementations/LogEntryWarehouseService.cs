using System.Collections.Concurrent;
using BgWorker.Services.Interfaces;
using Shared.Logging.Models;

namespace BgWorker.Services.Implementations;

public class LogEntryWarehouseService : ILogEntryWarehouseService
{
    private readonly ConcurrentQueue<LogEntryModel> _logEntries = [];

    public void AddLogEntry(LogEntryModel logEntry)
    {
        _logEntries.Enqueue(logEntry);
    }

    public int Count()
    {
        return _logEntries.Count;
    }

    public LogEntryModel? Dequeue()
    {
        if (_logEntries.TryDequeue(out var logEntry))
        {
            return logEntry;
        }

        return null;
    }

    public List<LogEntryModel> DrainList()
    {
        var list = new List<LogEntryModel>();
        while (_logEntries.TryDequeue(out var logEntry))
        {
            list.Add(logEntry);
        }

        return list;
    }
}