using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Shared.Logging.Models;
using Shared.Logging.Models.FileLog;

namespace Shared.Logging.Writer;

public class FileLogChannelWriter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Channel<LogEntryModel> _channel;
    private readonly FileLogChannelWriterConfiguration _options;
    private readonly ConcurrentQueue<string> _queue = [];

    public FileLogChannelWriter(FileLogChannelWriterConfiguration options)
    {
        _options = options;
        var channelOptions = new BoundedChannelOptions(10000) { FullMode = BoundedChannelFullMode.DropOldest };
        _channel = Channel.CreateBounded<LogEntryModel>(channelOptions);
    }

    public void Write(LogEntryModel logEntry)
    {
        _channel.Writer.TryWrite(logEntry);
    }

    private async Task ProcessChannelAsync()
    {
        try
        {
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _options.MaxParallelism };
            await Parallel.ForEachAsync(_channel.Reader.ReadAllAsync(), parallelOptions, async (logEntry, _) =>
            {
                try
                {
                    var line = JsonSerializer.Serialize(logEntry, LogEntryHelper.GetNonIntendOption);
                    _queue.Enqueue(line);

                    if (_queue.Count >= _options.BatchSize)
                    {
                        await WriteAsync();
                    }
                }
                catch (Exception)
                {
                    // Nothing
                }
            });
        }
        catch (Exception )
        {
            // Nothing
        }
    }

    private StreamWriter GetStreamWriter()
    {
        return new StreamWriter("");
    }

    private async Task WriteAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_queue.Count <= _options.BatchSize)
            {
                return;
            }

            var builder = new StringBuilder();
            while (_queue.TryDequeue(out var line))
            {
                builder.AppendLine(line);
            }

            var streamWriter = GetStreamWriter();
            await streamWriter.WriteAsync(builder);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}