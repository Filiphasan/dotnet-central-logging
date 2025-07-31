using BgWorker.Models;

namespace BgWorker.Services.Interfaces;

public interface IEcsLogEntryMemEstimateService
{
    void CalculateBytes(string key, EcsLogEntryModel logEntry);
    int GetAvgBulkApiRequestItemSize(string key);
}