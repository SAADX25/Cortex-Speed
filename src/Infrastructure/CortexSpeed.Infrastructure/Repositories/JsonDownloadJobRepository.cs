using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Enums;
using CortexSpeed.Domain.Interfaces;

namespace CortexSpeed.Infrastructure.Repositories;

public class JsonDownloadJobRepository : IDownloadJobRepository, IDisposable
{
    private readonly string _storageDir;
    private readonly string _storageFile;
    private ConcurrentDictionary<Guid, DownloadJob> _store = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        Converters = { new JsonStringEnumConverter() }
    };
    
    private readonly CancellationTokenSource _bgSaveCts = new();
    private readonly Task _bgSaveTask;

    public JsonDownloadJobRepository()
    {
        _storageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CortexSpeed");
        _storageFile = Path.Combine(_storageDir, "downloads.json");
        
        Directory.CreateDirectory(_storageDir);
        LoadData();
        
        // Background thread to save state periodically to persist segment progress
        _bgSaveTask = Task.Run(PeriodicSaveAsync);
    }

    private void LoadData()
    {
        try
        {
            if (File.Exists(_storageFile))
            {
                var json = File.ReadAllText(_storageFile);
                var jobs = JsonSerializer.Deserialize<List<DownloadJob>>(json, _jsonOptions);
                if (jobs != null)
                {
                    _store = new ConcurrentDictionary<Guid, DownloadJob>(jobs.ToDictionary(j => j.Id, j => j));
                    
                    // Reset downloading states to paused on startup
                    foreach (var job in _store.Values)
                    {
                        if (job.State == DownloadState.Downloading || job.State == DownloadState.Queued)
                        {
                            job.State = DownloadState.Paused;
                            foreach (var s in job.Segments)
                            {
                                if (s.State == DownloadState.Downloading || s.State == DownloadState.Queued)
                                    s.State = DownloadState.Paused;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            _store = new ConcurrentDictionary<Guid, DownloadJob>();
        }
    }

    private async Task PeriodicSaveAsync()
    {
        while (!_bgSaveCts.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), _bgSaveCts.Token);
                await SaveDataAsync();
            }
            catch (OperationCanceledException) { break; }
            catch { /* Ignore background save errors */ }
        }
    }

    private async Task SaveDataAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var jobs = _store.Values.ToList();
            var json = JsonSerializer.Serialize(jobs, _jsonOptions);
            
            // Write to a temporary file then move to avoid corruption if process crashes during write
            var tempFile = _storageFile + ".tmp";
            await File.WriteAllTextAsync(tempFile, json);
            File.Move(tempFile, _storageFile, true);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public Task<DownloadJob> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(jobId, out var job);
        return Task.FromResult(job); // Null if not found
    }

    public Task<IEnumerable<DownloadJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Values.AsEnumerable());
    }

    public async Task AddAsync(DownloadJob job, CancellationToken cancellationToken = default)
    {
        if (_store.TryAdd(job.Id, job))
        {
            await SaveDataAsync();
        }
    }

    public async Task UpdateAsync(DownloadJob job, CancellationToken cancellationToken = default)
    {
        _store[job.Id] = job;
        await SaveDataAsync();
    }

    public async Task RemoveAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        if (_store.TryRemove(jobId, out _))
        {
            await SaveDataAsync();
        }
    }

    public void Dispose()
    {
        if (!_bgSaveCts.IsCancellationRequested)
        {
            _bgSaveCts.Cancel();
            try { _bgSaveTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
            SaveDataAsync().GetAwaiter().GetResult();
            _bgSaveCts.Dispose();
            _fileLock.Dispose();
        }
    }
}