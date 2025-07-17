using System.Text.Json.Serialization.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Shared.Messaging.Models;

public class PublishMessageModel<TMessage> where TMessage : class
{
    public string? MessageId { get; set; }
    public PublishMessageExchangeModel Exchange { get; set; } = PublishMessageExchangeModel.Default;
    public string RoutingKey { get; set; } = string.Empty;
    public TMessage? Message { get; set; }
    public byte Priority { get; set; }
    public DeliveryModes DeliveryMode { get; set; } = DeliveryModes.Persistent;
    public byte TryCount { get; set; } = 2;
    public bool Mandatory { get; set; } = false;
    public Func<BasicReturnEventArgs, Task>? ReturnHandler { get; set; } = null;
    public IDictionary<string, object?>? Headers { get; set; }
    public JsonTypeInfo<TMessage>? JsonTypeInfo { get; set; } = null;
}

public class PublishMessageExchangeModel
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = ExchangeType.Fanout;
    public IDictionary<string, object?>? Arguments { get; set; }
    public bool Durable { get; set; } = true;
    public bool AutoDelete { get; set; } = false;

    public static PublishMessageExchangeModel Default => new();
}