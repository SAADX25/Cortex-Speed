using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Enums;
using CortexSpeed.Domain.Interfaces;
using MediatR;
using System.Collections.Concurrent;

namespace CortexSpeed.Application.Handlers;

public class StartDownloadCommandHandler : IRequestHandler<StartDownloadCommand, Guid>
{
    private readonly IDownloadEngine _downloadEngine;

    // In a full application, this would be injected as an IJobRepository
    // Using a static ConcurrentDictionary for lightweight in-memory state tracking
    public static readonly ConcurrentDictionary<Guid, DownloadJob> InMemoryJobStore = new();

    public StartDownloadCommandHandler(IDownloadEngine downloadEngine)
    {
        _downloadEngine = downloadEngine;
    }

    public async Task<Guid> Handle(StartDownloadCommand request, CancellationToken cancellationToken)
    {
        var destinationPath = Path.Combine(request.DestinationFolder, request.FileName);
        
        var job = new DownloadJob
        {
            Id = Guid.NewGuid(),
            Url = request.Url,
            DestinationFilePath = destinationPath,
            FileName = request.FileName,
            State = request.ScheduledAt.HasValue ? DownloadState.Scheduled : DownloadState.Queued,
            CreatedAt = DateTime.UtcNow,
            ScheduledAt = request.ScheduledAt,
            MaxSegments = request.MaxSegments,
            SpeedLimitBytesPerSecond = request.SpeedLimitBytesPerSecond,
            Category = DownloadJob.ClassifyByExtension(request.FileName)
        };

        InMemoryJobStore.TryAdd(job.Id, job);

        if (!request.ScheduledAt.HasValue)
        {
            // Fire-and-forget the engine orchestration so we don't block the UI thread waiting for the download
            _ = Task.Run(() => _downloadEngine.StartDownloadAsync(job, CancellationToken.None), CancellationToken.None);
        }

        return await Task.FromResult(job.Id);
    }
}
