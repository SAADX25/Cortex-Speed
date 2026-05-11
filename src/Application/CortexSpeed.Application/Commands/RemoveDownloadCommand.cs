using MediatR;

namespace CortexSpeed.Application.Commands;

public record RemoveDownloadCommand(Guid JobId, bool DeleteFile) : IRequest<bool>;
