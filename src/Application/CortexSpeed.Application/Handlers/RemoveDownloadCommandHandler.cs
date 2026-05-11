using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class RemoveDownloadCommandHandler : IRequestHandler<RemoveDownloadCommand, bool>
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly IFileSystemProvider _fileSystemProvider;

    public RemoveDownloadCommandHandler(IDownloadEngine downloadEngine, IFileSystemProvider fileSystemProvider)
    {
        _downloadEngine = downloadEngine;
        _fileSystemProvider = fileSystemProvider;
    }

    public async Task<bool> Handle(RemoveDownloadCommand request, CancellationToken cancellationToken)
    {
        if (StartDownloadCommandHandler.InMemoryJobStore.TryRemove(request.JobId, out var job))
        {
            // Cancel the download if it's still in progress
            if (job.State == Domain.Enums.DownloadState.Downloading || job.State == Domain.Enums.DownloadState.Queued)
            {
                await _downloadEngine.CancelDownloadAsync(request.JobId);
            }

            // Delete the file from disk if requested
            if (request.DeleteFile && _fileSystemProvider.FileExists(job.DestinationFilePath))
            {
                _fileSystemProvider.DeleteFile(job.DestinationFilePath);
            }

            return true;
        }
        return false;
    }
}
