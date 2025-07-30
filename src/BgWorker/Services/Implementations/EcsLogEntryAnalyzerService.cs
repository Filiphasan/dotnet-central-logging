using System.Collections.Concurrent;
using BgWorker.Models;
using BgWorker.Services.Interfaces;

namespace BgWorker.Services.Implementations;

public class EcsLogEntryAnalyzerService : IEcsLogEntryAnalyzerService
{
    public const int AnalyzedPeriodMs = 5 * 1000;

    private readonly ConcurrentDictionary<string, EcsLogEntryAnalyzDataModel> _calculatedList = new();

    public Task<bool> IsReadyToCalculate(string key)
    {
        var model = GetOrCreateModel(key);

        return Task.FromResult(DateTime.UtcNow.Subtract(model.LastAnalyzedDate).TotalMicroseconds >= AnalyzedPeriodMs);
    }

    public Task<int> CalculateBytes(string key, EcsLogEntryModel logEntry)
    {
        var model = GetOrCreateModel(key);
        model.LastAnalyzedDate = DateTime.UtcNow;
        
        // Serialize ile bytes öğrenme veya tahmin algoritması yapılacak sınıfn içindeki tiplere göre
        return Task.FromResult(0);
    }

    public Task<int> GetAvgBytes(string key)
    {
        var model = GetOrCreateModel(key);
        return Task.FromResult(model.AvgByteSize);
    }

    public Task<int> GetMaxBytes(string key)
    {
        var model = GetOrCreateModel(key);
        return Task.FromResult(model.MaxByteSize);
    }

    public Task<int> GetMinBytes(string key)
    {
        var model = GetOrCreateModel(key);
        return Task.FromResult(model.MinByteSize);
    }

    private EcsLogEntryAnalyzDataModel GetOrCreateModel(string key)
    {
        if (!_calculatedList.TryGetValue(key, out var model))
        {
            model = new EcsLogEntryAnalyzDataModel();
            _calculatedList.TryAdd(key, model);
        }

        return model;
    }
}

public sealed class EcsLogEntryAnalyzDataModel
{
    public DateTime LastAnalyzedDate { get; set; } = DateTime.UtcNow.AddMilliseconds(EcsLogEntryAnalyzerService.AnalyzedPeriodMs);
    public int AvgByteSize { get; set; }
    public int MaxByteSize { get; set; }
    public int MinByteSize { get; set; }
}