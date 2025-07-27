using System.IO.Compression;
using Microsoft.IO;
using Shared.Messaging.Services.Interfaces;

namespace Shared.Messaging.Services.Implementations;

public class BrotliCompressorService(RecyclableMemoryStreamManager recyclableMemoryStreamManager) : ICompressorService
{
    public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        await using var memoryStream = recyclableMemoryStreamManager.GetStream();
        await using (var brotli = new BrotliStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true))
        {
            await brotli.WriteAsync(data, cancellationToken);
        }

        return memoryStream.ToArray();
    }

    public async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
    {
        await using var memoryStream = recyclableMemoryStreamManager.GetStream("Decompress", data, 0, data.Length);
        await using var brotli = new BrotliStream(memoryStream, CompressionMode.Decompress);
        await using var decompressedStream = recyclableMemoryStreamManager.GetStream("DecompressedOutput");
        await brotli.CopyToAsync(decompressedStream, cancellationToken);

        return decompressedStream.ToArray();
    }
}