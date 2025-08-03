namespace Shared.Logging.Models.FileLog;

public class FileLogChannelWriterConfiguration
{
    public int MaxParallelism { get; set; } = 20;
    public int BatchSize { get; set; } = 100;
}