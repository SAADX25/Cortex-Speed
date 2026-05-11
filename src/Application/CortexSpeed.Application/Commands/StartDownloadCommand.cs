using CortexSpeed.Domain.Enums;
using MediatR;

namespace CortexSpeed.Application.Commands;

public record StartDownloadCommand(
    string Url, 
    string DestinationFolder, 
    string FileName,
    int MaxSegments = 16,
    long SpeedLimitBytesPerSecond = 0,
    DateTime? ScheduledAt = null) : IRequest<Guid>;
