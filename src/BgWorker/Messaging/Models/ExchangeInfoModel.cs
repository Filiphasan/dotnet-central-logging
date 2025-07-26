namespace BgWorker.Messaging.Models;

public class ExchangeInfoModel
{
    public string Name { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string ExchangeType { get; set; } = RabbitMQ.Client.ExchangeType.Direct;
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; } = false;
    public IDictionary<string, object?>? Arguments { get; set; } = null;

    public static ExchangeInfoModel Default => new();
}