using MediatR;

namespace CortexSpeed.Application.Commands;

public record PauseDownloadCommand(Guid JobId) : IRequest<bool>;
