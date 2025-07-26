using System.Text.Json.Serialization;

namespace BgWorker.Models;

public class EcsLogEntryModel : IElasticBulkModel
{
    [JsonIgnore] public string Id { get; set; } = Ulid.NewUlid().ToString().ToLower();

    [JsonPropertyName("@timestamp")] public DateTime Timestamp { get; set; }

    [JsonPropertyName("message")] public string? Message { get; set; }

    [JsonPropertyName("log")] public EcsLog Log { get; set; } = new();

    [JsonPropertyName("event")] public EcsEvent Event { get; set; } = new();

    [JsonPropertyName("error")] public EcsError? Error { get; set; }

    [JsonPropertyName("labels")] public Dictionary<string, string>? Enrichers { get; set; }

    [JsonPropertyName("fields")] public Dictionary<string, object?>? Properties { get; set; }
}

public class EcsLog
{
    [JsonPropertyName("level")] public string Level { get; set; } = "Information";

    [JsonPropertyName("logger")] public string? Logger { get; set; }
}

public class EcsEvent
{
    [JsonPropertyName("id")] public string? Id { get; set; }

    [JsonPropertyName("action")] public string? Action { get; set; }

    [JsonPropertyName("reference")] public string? Reference { get; set; }
}

public class EcsError
{
    [JsonPropertyName("type")] public string? Type { get; set; }

    [JsonPropertyName("message")] public string? Message { get; set; }

    [JsonPropertyName("stackTrace")] public string? StackTrace { get; set; }
}