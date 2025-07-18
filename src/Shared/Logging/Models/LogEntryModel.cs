using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Shared.Logging.Models;

public class LogEntryModel
{
    public string LogKey { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = nameof(LogLevel.Information);
    public string? Source { get; set; }
    public int? EventId { get; set; }
    public string? EventName { get; set; }
    public string? Message { get; set; }
    public ExceptionDetailModel? Exception { get; set; }
    public Dictionary<string, string> Enrichers { get; set; } = new();
    public Dictionary<string, object?>? Properties { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(LogEntryModel))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(ExceptionDetailModel))]
public partial class LogEntryModelJsonContext : JsonSerializerContext
{
}

public static class LogEntryHelper
{
    private static readonly Lazy<JsonSerializerOptions> IntendOptionLazy = new(() => new JsonSerializerOptions
    {
        WriteIndented = true,
        TypeInfoResolver = LogEntryModelJsonContext.Default
    });

    private static readonly Lazy<JsonSerializerOptions> NonIntendOptionLazy = new(() => new JsonSerializerOptions
    {
        WriteIndented = false,
        TypeInfoResolver = LogEntryModelJsonContext.Default
    });

    public static JsonSerializerOptions GetIntendOption => IntendOptionLazy.Value;
    public static JsonSerializerOptions GetNonIntendOption => NonIntendOptionLazy.Value;
}