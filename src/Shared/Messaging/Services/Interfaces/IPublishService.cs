using Shared.Messaging.Models;

namespace Shared.Messaging.Services.Interfaces;

public interface IPublishService
{
    Task PublishAsync<T>(PublishMessageModel<T> message, CancellationToken cancellationToken = default) where T : class;
}