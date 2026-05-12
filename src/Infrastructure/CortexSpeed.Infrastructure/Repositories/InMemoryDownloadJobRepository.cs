using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CortexSpeed.Infrastructure.Repositories;

public class InMemoryDownloadJobRepository : IDownloadJobRepository
{
    private readonly ConcurrentDictionary<Guid, DownloadJob> _store = new();

    public Task<DownloadJob> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(jobId, out var job);
        return Task.FromResult(job); // will return null if not found
    }

    public Task<IEnumerable<DownloadJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Values.AsEnumerable());
    }

    public Task AddAsync(DownloadJob job, CancellationToken cancellationToken = default)
    {
        _store.TryAdd(job.Id, job);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DownloadJob job, CancellationToken cancellationToken = default)
    {
        // For in-memory reference types, objects are usually updated directly in memory via reference.
        // But we provide this explicitly for semantics.
        _store[job.Id] = job;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(jobId, out _);
        return Task.CompletedTask;
    }
}