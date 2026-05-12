using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Enums;
using CortexSpeed.Domain.Interfaces;
using System.Buffers;

namespace CortexSpeed.Infrastructure.Network;

public class SegmentDownloader : ISegmentDownloader
{
    public async Task DownloadSegmentAsync(string url, DownloadSegment segment, IProtocolHandler protocolHandler, Stream destinationStream, CancellationToken cancellationToken)
    {
        int maxRetries = 5;
        int currentTry = 0;

        while (currentTry < maxRetries)
        {
            try
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
                        long bytesRemaining = segment.EndOffset - segment.CurrentOffset + 1;
                        if (bytesRemaining <= 0) break; // Reached the end of the segment

                        int bytesToWrite = (int)Math.Min(bytesRead, bytesRemaining);
                        await destinationStream.WriteAsync(buffer, 0, bytesToWrite, cancellationToken);
                        
                        segment.CurrentOffset += bytesToWrite;

                        if (segment.CurrentOffset > segment.EndOffset)
                        {
                            break; // Segment download complete
                        }
                        
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

                // Exit retry loop if successful
                break;
            }
            catch (OperationCanceledException)
            {
                // Don't retry on cancellation
                break;
            }
            catch (Exception)
            {
                currentTry++;
                if (currentTry >= maxRetries)
                {
                    throw;
                }
                
                // Wait for 3 seconds before retrying from the current offset
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}
