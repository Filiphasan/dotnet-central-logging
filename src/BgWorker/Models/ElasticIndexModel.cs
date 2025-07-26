namespace BgWorker.Models;

public class ElasticIndexModel<T> where T : class
{
    public string? Index { get; set; }
    public T? Data { get; set; }
}