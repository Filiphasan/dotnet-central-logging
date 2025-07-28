using BgWorker.Models;

namespace BgWorker.Services.Interfaces;

public interface IFailedEcsLogEntryWarehouseService
{
    void AddLogEntry(EcsLogEntryModel model);
    void AddLogEntries(IEnumerable<EcsLogEntryModel> models);
    int Count();
    EcsLogEntryModel? Dequeue();
    List<EcsLogEntryModel> DrainList();
}