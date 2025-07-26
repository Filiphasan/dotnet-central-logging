using BgWorker.Models;

namespace BgWorker.Services.Interfaces;

public interface IElasticService
{
    Task IndexAsync<T>(ElasticIndexModel<T> model, CancellationToken cancellationToken = default) where T : class;
    Task<List<T>> BulkAsync<T>(ElasticBulkModel<T> model, CancellationToken cancellationToken = default) where T : IElasticBulkModel;
}