using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Enums;
using CortexSpeed.Domain.Interfaces;
using System.Buffers;

namespace CortexSpeed.Infrastructure.Network;

public class SegmentDownloader : ISegmentDownloader
{
    public async Task DownloadSegmentAsync(string url, DownloadSegment segment, IProtocolHandler protocolHandler, Stream destinationStream, CancellationToken cancellationToken)
    {
        // 1. Fetch the exact byte-range from the server
        using var networkStream = await protocolHandler.GetStreamAsync(url, segment.CurrentOffset, segment.EndOffset, cancellationToken);
        
        // 2. Seek the disk stream to the correct location (Thread-safe due to RandomAccess / distinct stream instances)
        destinationStream.Position = segment.CurrentOffset;

        // 3. High-performance buffered read/write loop using rented memory to avoid GC spikes
        var buffer = ArrayPool<byte>.Shared.Rent(81920); // 80KB buffer size
        try
        {
            int bytesRead;
            while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destinationStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                
                segment.CurrentOffset += bytesRead;
                
                if (cancellationToken.IsCancellationRequested)
                {
                    segment.State = DownloadState.Paused;
                    break;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
