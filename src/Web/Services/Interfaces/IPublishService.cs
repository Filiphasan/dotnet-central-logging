using Web.Common.Models.Messaging;

namespace Web.Services.Interfaces;

public interface IPublishService
{
    Task PublishAsync<T>(PublishMessageModel<T> message, CancellationToken cancellationToken = default) where T : class;
}