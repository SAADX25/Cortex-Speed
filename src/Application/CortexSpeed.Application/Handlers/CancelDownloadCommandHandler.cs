using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class CancelDownloadCommandHandler : IRequestHandler<CancelDownloadCommand, bool>
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly IDownloadJobRepository _jobRepository;

    public CancelDownloadCommandHandler(IDownloadEngine downloadEngine, IDownloadJobRepository jobRepository)
    {
        _downloadEngine = downloadEngine;
        _jobRepository = jobRepository;
    }

    public async Task<bool> Handle(CancelDownloadCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job != null)
        {
            await _downloadEngine.CancelDownloadAsync(request.JobId);
            return true;
        }
        return false;
    }
}
