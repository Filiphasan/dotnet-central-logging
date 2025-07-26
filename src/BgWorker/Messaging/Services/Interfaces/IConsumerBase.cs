using BgWorker.Messaging.Models;

namespace BgWorker.Messaging.Services.Interfaces;

public interface IConsumerBase<in TModel> where TModel : class
{
    int ConsumerCount { get; }
    ConsumeInfoModel GetConsumeInfo();
    Task<ConsumeResult> ConsumeAsync(TModel model, CancellationToken cancellationToken = default);
}