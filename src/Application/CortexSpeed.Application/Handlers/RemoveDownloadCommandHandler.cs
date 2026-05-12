using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class RemoveDownloadCommandHandler : IRequestHandler<RemoveDownloadCommand, bool>
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly IFileSystemProvider _fileSystemProvider;
    private readonly IDownloadJobRepository _jobRepository;

    public RemoveDownloadCommandHandler(IDownloadEngine downloadEngine, IFileSystemProvider fileSystemProvider, IDownloadJobRepository jobRepository)
    {
        _downloadEngine = downloadEngine;
        _fileSystemProvider = fileSystemProvider;
        _jobRepository = jobRepository;
    }

    public async Task<bool> Handle(RemoveDownloadCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job != null)
        {
            // Cancel the download if it's still in progress
            if (job.State == Domain.Enums.DownloadState.Downloading || job.State == Domain.Enums.DownloadState.Queued)
            {
                await _downloadEngine.CancelDownloadAsync(request.JobId);
            }

            // Remove from the repository
            await _jobRepository.RemoveAsync(request.JobId, cancellationToken);

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
