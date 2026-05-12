using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Enums;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class StartDownloadCommandHandler : IRequestHandler<StartDownloadCommand, Guid>
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly IDownloadJobRepository _jobRepository;

    public StartDownloadCommandHandler(IDownloadEngine downloadEngine, IDownloadJobRepository jobRepository)
    {
        _downloadEngine = downloadEngine;
        _jobRepository = jobRepository;
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

        await _jobRepository.AddAsync(job, cancellationToken);

        if (!request.ScheduledAt.HasValue)
        {
            // Fire-and-forget the engine orchestration so we don't block the UI thread waiting for the download
            _ = Task.Run(() => _downloadEngine.StartDownloadAsync(job, CancellationToken.None), CancellationToken.None);
        }

        return job.Id;
    }
}
