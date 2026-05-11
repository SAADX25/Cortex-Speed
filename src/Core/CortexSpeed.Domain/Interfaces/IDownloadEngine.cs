using CortexSpeed.Domain.Entities;

namespace CortexSpeed.Domain.Interfaces;

public interface IDownloadEngine
{
    Task StartDownloadAsync(DownloadJob job, CancellationToken cancellationToken);
    Task PauseDownloadAsync(Guid jobId);
    Task ResumeDownloadAsync(Guid jobId);
    Task CancelDownloadAsync(Guid jobId);
}
