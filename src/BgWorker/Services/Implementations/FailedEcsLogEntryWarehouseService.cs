using System.Collections.Concurrent;
using BgWorker.Models;
using BgWorker.Services.Interfaces;

namespace BgWorker.Services.Implementations;

public class FailedEcsLogEntryWarehouseService : IFailedEcsLogEntryWarehouseService
{
    private readonly ConcurrentQueue<EcsLogEntryModel> _logEntries = [];

    public void AddLogEntry(EcsLogEntryModel model)
    {
        _logEntries.Enqueue(model);
    }

    public void AddLogEntries(IEnumerable<EcsLogEntryModel> models)
    {
        foreach (var ecsLogEntryModel in models)
        {
            _logEntries.Enqueue(ecsLogEntryModel);
        }
    }

    public int Count()
    {
        return _logEntries.Count;
    }

    public EcsLogEntryModel? Dequeue()
    {
        if (_logEntries.TryDequeue(out var logEntry))
        {
            return logEntry;
        }

        return null;
    }

    public List<EcsLogEntryModel> DrainList()
    {
        var list = new List<EcsLogEntryModel>();
        while (_logEntries.TryDequeue(out var logEntry))
        {
            list.Add(logEntry);
        }

        return list;
    }
}