using BgWorker.Messaging.Models;

namespace BgWorker.Messaging.Services.Interfaces;

public interface IMqConsumeService
{
    Task ConsumeAsync<TModel>(IConsumerBase<TModel> consume, CancellationToken cancellationToken = default) where TModel : class;
    Task ConsumeAsync<TModel>(ConsumeInfoModel consumeInfo, Func<TModel, CancellationToken, Task<ConsumeResult>> consume, CancellationToken cancellationToken = default) where TModel : class;
    Task ConsumeAsync<TModel>(ConsumeInfoModel consumeInfo, Func<TModel, IServiceScope, CancellationToken, Task<ConsumeResult>> consume, CancellationToken cancellationToken = default) where TModel : class;
}