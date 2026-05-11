using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class PauseDownloadCommandHandler : IRequestHandler<PauseDownloadCommand, bool>
{
    private readonly IDownloadEngine _downloadEngine;

    public PauseDownloadCommandHandler(IDownloadEngine downloadEngine)
    {
        _downloadEngine = downloadEngine;
    }

    public async Task<bool> Handle(PauseDownloadCommand request, CancellationToken cancellationToken)
    {
        if (StartDownloadCommandHandler.InMemoryJobStore.TryGetValue(request.JobId, out var job))
        {
            await _downloadEngine.PauseDownloadAsync(request.JobId);
            return true;
        }
        return false;
    }
}
