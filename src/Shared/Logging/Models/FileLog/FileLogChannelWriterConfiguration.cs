namespace Shared.Logging.Models.FileLog;

public class FileLogChannelWriterConfiguration
{
    public int MaxParallelism { get; set; } = 20;
    public int WriteSize { get; set; } = 100;
    public int WriteInterval { get; set; } = 2_000;
    public string BaseFolder { get; set; } = "Logs";
}