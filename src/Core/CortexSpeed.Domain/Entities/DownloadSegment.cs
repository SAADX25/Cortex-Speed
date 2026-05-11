using CortexSpeed.Domain.Enums;

namespace CortexSpeed.Domain.Entities;

public class DownloadSegment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public long StartOffset { get; set; }
    public long EndOffset { get; set; }
    public long CurrentOffset { get; set; }
    public DownloadState State { get; set; }
    public long DownloadedBytes => CurrentOffset - StartOffset;
    public long TotalBytes => EndOffset - StartOffset + 1;
    public bool IsCompleted => State == DownloadState.Completed;
}
