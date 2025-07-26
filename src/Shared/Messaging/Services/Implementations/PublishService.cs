using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Messaging.Models;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Messaging.Services.Implementations;

public class PublishService(IChannelPoolService poolService, ICompressorService compressorService) : IPublishService
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
            var serializedBody = JsonSerializer.Serialize(message.Message, message.JsonSerializerOptions);
            var body = Encoding.UTF8.GetBytes(serializedBody);
            if (message.CompressMessage)
            {
                body = await compressorService.CompressAsync(body, cancellationToken);
            }

            await DeclareExchangeIfNeedAsync(channel, message.Exchange, cancellationToken);

            await channel.BasicPublishAsync(
                exchange: message.Exchange.Name,
                routingKey: message.RoutingKey,
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

    private static async Task DeclareExchangeIfNeedAsync(IChannel channel, PublishMessageExchangeModel exchange, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(exchange.Name))
        {
            return;
        }

        await channel.ExchangeDeclareAsync(exchange.Name, exchange.Type, exchange.Durable, exchange.AutoDelete, exchange.Arguments, cancellationToken: cancellationToken);
    }
}