using CortexSpeed.Domain.Entities;

namespace CortexSpeed.Domain.Interfaces;

public interface ISegmentDownloader
{
    Task DownloadSegmentAsync(string url, DownloadSegment segment, IProtocolHandler protocolHandler, Stream destinationStream, CancellationToken cancellationToken);
}
