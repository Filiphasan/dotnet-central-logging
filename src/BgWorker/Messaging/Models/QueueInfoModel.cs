namespace BgWorker.Messaging.Models;

public class QueueInfoModel
{
    public string Name { get; set; } = string.Empty;
    public bool Durable { get; set; } = true;
    public bool Exclusive { get; set; } = false;
    public bool AutoDelete { get; set; } = false;
    public IDictionary<string, object?>? Arguments { get; set; } = null;

    public static QueueInfoModel Default => new();
}