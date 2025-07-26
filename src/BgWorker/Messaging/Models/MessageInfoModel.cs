using System.Text.Json;

namespace BgWorker.Messaging.Models;

public class MessageInfoModel
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool Decompress { get; set; } = false;
    public JsonSerializerOptions? SerializerOptions { get; set; }

    public static MessageInfoModel Default => new();
}