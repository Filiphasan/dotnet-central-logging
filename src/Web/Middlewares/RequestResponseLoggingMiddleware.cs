using System.Diagnostics;
using Microsoft.IO;

namespace Web.Middlewares;

public class RequestResponseLoggingMiddleware(ILogger<RequestResponseLoggingMiddleware> logger, RecyclableMemoryStreamManager recyclableMemoryStreamManager) : IMiddleware
{
    private static readonly string[] ExcludedPaths =
    [
        "/doc",
        "/docs",
        "/openapi",
        "/scalar",
        "/index.html"
    ];
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path;
        if (ExcludedPaths.Any(x => path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var correlationId = context.Request.Headers[CorrelationMiddleware.CorrelationIdKey].FirstOrDefault() ?? Guid.NewGuid().ToString();
        await LogRequest(context, correlationId);

        var originalBodyStream = context.Response.Body;

        await using var responseBody = recyclableMemoryStreamManager.GetStream();
        context.Response.Body = responseBody;

        var stopwatch = Stopwatch.StartNew();

        await next(context);

        stopwatch.Stop();

        await LogResponse(context, stopwatch.ElapsedMilliseconds, correlationId);

        await responseBody.CopyToAsync(originalBodyStream);
    }

    private async Task LogRequest(HttpContext context, string correlationId)
    {
        var request = context.Request;

        request.EnableBuffering();

        var bodyAsText = await ReadStreamInChunks(request.Body);

        request.Body.Position = 0;

        var headerList = new List<string>();
        foreach (var (key, value) in request.Headers)
        {
            headerList.Add($"{key}: {value}");
        }

        var headerStr = string.Join(", ", headerList);
        logger.LogInformation("Request {CorrelationId} {Method} {Path} {QueryStr} {Headers} {Body}", correlationId, request.Method, request.Path, request.QueryString, headerStr, bodyAsText);
    }

    private async Task LogResponse(HttpContext context, long duration, string correlationId)
    {
        var response = context.Response;

        response.Body.Seek(0, SeekOrigin.Begin);

        var bodyAsText = await new StreamReader(response.Body).ReadToEndAsync();

        response.Body.Seek(0, SeekOrigin.Begin);

        var headerList = new List<string>();
        foreach (var (key, value) in response.Headers)
        {
            headerList.Add($"{key}: {value}");
        }

        var headerStr = string.Join(", ", headerList);
        logger.LogInformation("Response {CorrelationId} {StatusCode} {Duration} {Headers} {Body}", correlationId, response.StatusCode, duration, headerStr, bodyAsText);
    }

    private static async Task<string> ReadStreamInChunks(Stream stream)
    {
        const int readChunkBufferLength = 4096;
        stream.Seek(0, SeekOrigin.Begin);
        await using var textWriter = new StringWriter();
        using var reader = new StreamReader(stream, leaveOpen: true);
        var readChunk = new char[readChunkBufferLength];
        int readChars;
        while ((readChars = await reader.ReadBlockAsync(readChunk, 0, readChunkBufferLength)) > 0)
        {
            await textWriter.WriteAsync(readChunk, 0, readChars);
        }

        return textWriter.ToString();
    }
}