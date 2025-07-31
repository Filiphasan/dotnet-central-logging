using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using BgWorker.Models;
using BgWorker.Services.Interfaces;

namespace BgWorker.Services.Implementations;

public class EcsLogEntryMemEstimateService : IEcsLogEntryMemEstimateService
{
    private const int EcsExpectedByteSize = 10 * 1024 * 1024;

    private readonly ConcurrentDictionary<string, EcsLogEntryAnalyzDataModel> _calculatedList = new();

    public void CalculateBytes(string key, EcsLogEntryModel logEntry)
    {
        var model = GetOrCreateModel(key);
        var serialized = JsonSerializer.Serialize(logEntry);
        var bytes = Encoding.UTF8.GetBytes(serialized);
        model.CalcByteSizes(bytes.Length);
    }

    public int GetAvgBulkApiRequestItemSize(string key)
    {
        var model = GetOrCreateModel(key);
        return EcsExpectedByteSize / model.AvgByteSize;
    }

    private EcsLogEntryAnalyzDataModel GetOrCreateModel(string key)
    {
        return _calculatedList.GetOrAdd(key, new EcsLogEntryAnalyzDataModel());
    }
}

public sealed class EcsLogEntryAnalyzDataModel
{
    public int AnalyzedCount { get; private set; }
    public int AvgByteSize { get; private set; }
    public int MaxByteSize { get; private set; }
    public int MinByteSize { get; private set; }

    public void CalcByteSizes(int bytes)
    {
        AvgByteSize = (AvgByteSize * AnalyzedCount + bytes) / AnalyzedCount;
        MaxByteSize = Math.Max(MaxByteSize, bytes);
        MinByteSize = Math.Min(MinByteSize, bytes);
        AnalyzedCount++;
    }
}