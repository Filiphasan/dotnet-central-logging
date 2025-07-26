using Shared.Logging.Models;

namespace BgWorker.Services.Interfaces;

public interface ILogEntryWarehouseService
{
    void AddLogEntry(LogEntryModel logEntry);
    int Count();
    LogEntryModel? Dequeue();
    List<LogEntryModel> DrainList();
}