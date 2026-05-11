using MediatR;

namespace CortexSpeed.Application.Commands;

public record ResumeDownloadCommand(Guid JobId) : IRequest<bool>;
