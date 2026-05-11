namespace CortexSpeed.Domain.Enums;

public enum DownloadState
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Error,
    Canceled,
    Assembling,
    Scheduled,
    Waiting // Waiting in queue for a slot
}
