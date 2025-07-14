using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Services.Interfaces;
using Web.Common.Models.Messaging;
using Web.Services.Interfaces;

namespace Web.Services.Implementations;

public class PublishService(IChannelPoolService poolService) : IPublishService
{
    public async Task PublishAsync<T>(PublishMessageModel<T> message, CancellationToken cancellationToken = default) where T : class
    {
        var channel = await poolService.GetChannelAsync(cancellationToken);
        AsyncEventHandler<BasicReturnEventArgs>? returnHandler = null;

        try
        {
            ArgumentNullException.ThrowIfNull(message.Message);
            if (message.Mandatory)
            {
                ArgumentNullException.ThrowIfNull(message.ReturnHandler);
                returnHandler = async (_, args) => await message.ReturnHandler(args);
                channel.BasicReturnAsync += returnHandler;
            }

            var properties = new BasicProperties
            {
                Priority = message.Priority,
                DeliveryMode = message.DeliveryMode,
                MessageId = message.MessageId,
                Headers = message.Headers,
            };
            var serializedBody = message.JsonTypeInfo is null
                ? JsonSerializer.Serialize(message.Message)
                : JsonSerializer.Serialize(message.Message, message.JsonTypeInfo);
            var body = Encoding.UTF8.GetBytes(serializedBody);

            await channel.BasicPublishAsync(
                exchange: message.ExchangeName,
                routingKey: string.IsNullOrEmpty(message.ExchangeName) ? message.QueueName : string.Empty,
                mandatory: message.Mandatory,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception)
        {
            if (message.TryCount > 0)
            {
                message.TryCount--;
                await PublishAsync(message, cancellationToken);
            }
            else
            {
                throw;
            }
        }
        finally
        {
            if (returnHandler is not null)
            {
                channel.BasicReturnAsync -= returnHandler;
            }

            await poolService.ReturnChannelAsync(channel, cancellationToken);
        }
    }
}