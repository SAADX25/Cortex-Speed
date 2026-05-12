using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class PauseDownloadCommandHandler : IRequestHandler<PauseDownloadCommand, bool>
{
    private readonly IDownloadEngine _downloadEngine;
    private readonly IDownloadJobRepository _jobRepository;

    public PauseDownloadCommandHandler(IDownloadEngine downloadEngine, IDownloadJobRepository jobRepository)
    {
        _downloadEngine = downloadEngine;
        _jobRepository = jobRepository;
    }

    public async Task<bool> Handle(PauseDownloadCommand request, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(request.JobId, cancellationToken);
        if (job != null)
        {
            await _downloadEngine.PauseDownloadAsync(request.JobId);
            return true;
        }
        return false;
    }
}
