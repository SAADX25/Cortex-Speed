using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;

namespace CortexSpeed.Application.Handlers;

public class CancelDownloadCommandHandler : IRequestHandler<CancelDownloadCommand, bool>
{
    private readonly IDownloadEngine _downloadEngine;

    public CancelDownloadCommandHandler(IDownloadEngine downloadEngine)
    {
        _downloadEngine = downloadEngine;
    }

    public async Task<bool> Handle(CancelDownloadCommand request, CancellationToken cancellationToken)
    {
        if (StartDownloadCommandHandler.InMemoryJobStore.TryGetValue(request.JobId, out var job))
        {
            await _downloadEngine.CancelDownloadAsync(request.JobId);
            return true;
        }
        return false;
    }
}
