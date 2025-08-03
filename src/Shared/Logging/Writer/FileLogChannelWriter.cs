using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shared.Logging.Helpers;
using Shared.Logging.Models;
using Shared.Logging.Models.FileLog;

namespace Shared.Logging.Writer;

public sealed class FileLogChannelWriter
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly Channel<LogEntryModel> _channel;
    private readonly FileLogChannelWriterConfiguration _options;
    private readonly ConsoleBeautifyChannelWriter _consoleBeautifyChannelWriter;
    private readonly ConcurrentQueue<string> _queue = [];

    private StreamWriter? _streamWriter;
    private DateTime _writerDate = DateTime.UtcNow;
    private DateTime _lastWriteDate = DateTime.UtcNow;

    public FileLogChannelWriter(FileLogChannelWriterConfiguration options, ConsoleBeautifyChannelWriter consoleBeautifyChannelWriter)
    {
        _options = options;
        _consoleBeautifyChannelWriter = consoleBeautifyChannelWriter;
        var channelOptions = new BoundedChannelOptions(10000) { FullMode = BoundedChannelFullMode.DropOldest };
        _channel = Channel.CreateBounded<LogEntryModel>(channelOptions);
        Task.Run(ProcessChannelAsync);
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

                    if (_queue.Count >= _options.WriteSize || _lastWriteDate.Subtract(DateTime.UtcNow).TotalMilliseconds >= _options.WriteInterval)
                    {
                        _lastWriteDate = DateTime.UtcNow;
                        await WriteAsync();
                    }
                }
                catch (Exception ex)
                {
                    HandleProcessChannelException(ex);
                }
            });
        }
        catch (Exception ex)
        {
            HandleProcessChannelException(ex);
        }
    }

    private void HandleProcessChannelException(Exception exception)
    {
        _consoleBeautifyChannelWriter.Write(new LogEntryModel
        {
            Timestamp = DateTime.UtcNow,
            Level = nameof(LogLevel.Error),
            Source = "Shared.Logging.Writer.FileLogChannelWriter",
            Message = "FileLogChannelWriter Critical Error",
            Exception = LoggerHelper.ExtractExceptionDetail(exception),
            Properties = null,
        });
    }

    private async Task<StreamWriter> GetStreamWriterAsync()
    {
        var utcNow = DateTime.UtcNow;
        if (_streamWriter is not null && _writerDate.Date == utcNow.Date && _writerDate.Hour == utcNow.Hour)
        {
            return _streamWriter;
        }

        if (_streamWriter is not null)
        {
            await _streamWriter.DisposeAsync();
        }

        var folderPath = Path.Combine(AppContext.BaseDirectory, _options.BaseFolder, utcNow.Year.ToString(), utcNow.Month.ToString(), utcNow.Day.ToString());
        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, $"log-{utcNow:yyyyMMdd-HH}.log");

        _streamWriter = new StreamWriter(filePath, append: true, Encoding.UTF8, 64 * 1024) { AutoFlush = true }; // Toplu yazma yaptığımdan AutoFlush true ayarlı
        _writerDate = utcNow;
        return _streamWriter;
    }

    private async Task WriteAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_queue.IsEmpty)
            {
                return;
            }

            var builder = new StringBuilder();
            while (_queue.TryDequeue(out var line))
            {
                builder.AppendLine(line);
            }

            var streamWriter = await GetStreamWriterAsync();
            await streamWriter.WriteAsync(builder);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}