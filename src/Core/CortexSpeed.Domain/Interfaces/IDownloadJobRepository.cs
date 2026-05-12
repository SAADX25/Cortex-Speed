using CortexSpeed.Domain.Entities;

namespace CortexSpeed.Domain.Interfaces;

public interface IDownloadJobRepository
{
    Task<DownloadJob> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DownloadJob>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(DownloadJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(DownloadJob job, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid jobId, CancellationToken cancellationToken = default);
}