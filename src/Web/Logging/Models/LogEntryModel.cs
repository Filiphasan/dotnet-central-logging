using System.Text.Json.Serialization;

namespace Web.Logging.Models;

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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(LogEntryModel))]
public partial class LogEntryModelJsonContext : JsonSerializerContext
{
}