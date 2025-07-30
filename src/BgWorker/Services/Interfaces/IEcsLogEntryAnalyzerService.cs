using BgWorker.Models;

namespace BgWorker.Services.Interfaces;

public interface IEcsLogEntryAnalyzerService
{
    Task<bool> IsReadyToCalculate(string key);
    Task<int> CalculateBytes(string key, EcsLogEntryModel logEntry);
    Task<int> GetAvgBytes(string key);
    Task<int> GetMaxBytes(string key);
    Task<int> GetMinBytes(string key);
}