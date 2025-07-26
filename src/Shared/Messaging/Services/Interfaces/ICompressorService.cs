namespace Shared.Messaging.Services.Interfaces;

public interface ICompressorService
{
    Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);
    Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default);
}