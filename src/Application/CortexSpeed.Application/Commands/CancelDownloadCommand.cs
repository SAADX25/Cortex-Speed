using MediatR;

namespace CortexSpeed.Application.Commands;

public record CancelDownloadCommand(Guid JobId) : IRequest<bool>;
