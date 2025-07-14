using System.Text.Json.Serialization.Metadata;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Web.Common.Models.Messaging;

public class PublishMessageModel<TMessage> where TMessage : class
{
    public string? MessageId { get; set; }
    public string ExchangeName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public TMessage? Message { get; set; }
    public byte Priority { get; set; }
    public DeliveryModes DeliveryMode { get; set; } = DeliveryModes.Persistent;
    public byte TryCount { get; set; } = 2;
    public bool Mandatory { get; set; } = false;
    public Func<BasicReturnEventArgs, Task>? ReturnHandler { get; set; } = null;
    public IDictionary<string, object?>? Headers { get; set; }
    public JsonTypeInfo<TMessage>? JsonTypeInfo { get; set; } = null;
}