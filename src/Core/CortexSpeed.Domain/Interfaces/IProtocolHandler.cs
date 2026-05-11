using CortexSpeed.Domain.Entities;

namespace CortexSpeed.Domain.Interfaces;

/// <summary>
/// Defines the contract for different protocol handlers (HTTP, FTP, Torrent, etc.).
/// This ensures modularity and easy extension.
/// </summary>
public interface IProtocolHandler
{
    string ProtocolScheme { get; }
    Task<long> GetFileSizeAsync(string url, CancellationToken cancellationToken);
    Task<bool> SupportsRangeRequestsAsync(string url, CancellationToken cancellationToken);
    Task<Stream> GetStreamAsync(string url, long startOffset, long endOffset, CancellationToken cancellationToken);
}
