using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using BgWorker.Models;
using BgWorker.Services.Interfaces;

namespace BgWorker.Services.Implementations;

public class EcsLogEntryMemEstimateService : IEcsLogEntryMemEstimateService
{
    private const int EcsExpectedByteSize = 15 * 1024 * 1024; // 15MB

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
    private int AnalyzedCount { get; set; }
    public int AvgByteSize { get; private set; }
    private int MaxByteSize { get; set; }
    private int MinByteSize { get; set; }

    public void CalcByteSizes(int bytes)
    {
        AnalyzedCount++;
        AvgByteSize = (AvgByteSize * AnalyzedCount + bytes) / AnalyzedCount;
        MaxByteSize = Math.Max(MaxByteSize, bytes);
        MinByteSize = Math.Min(MinByteSize, bytes);
    }
}