using CortexSpeed.Domain.Entities;
using CortexSpeed.Domain.Enums;
using CortexSpeed.Domain.Interfaces;
using System.Collections.Concurrent;

namespace CortexSpeed.Application.Services;

public class DownloadEngine : IDownloadEngine
{
    private readonly IEnumerable<IProtocolHandler> _protocolHandlers;
    private readonly ISegmentDownloader _segmentDownloader;
    private readonly IFileSystemProvider _fileSystemProvider;

    // Track CancellationTokenSources per-job for Pause/Cancel support
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCancellationTokens = new();

    public DownloadEngine(
        IEnumerable<IProtocolHandler> protocolHandlers,
        ISegmentDownloader segmentDownloader,
        IFileSystemProvider fileSystemProvider)
    {
        _protocolHandlers = protocolHandlers;
        _segmentDownloader = segmentDownloader;
        _fileSystemProvider = fileSystemProvider;
    }

    public async Task StartDownloadAsync(DownloadJob job, CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _jobCancellationTokens[job.Id] = cts;
        
        job.State = DownloadState.Downloading;

        // Find appropriate protocol handler (e.g., HTTP vs Torrent)
        var handler = _protocolHandlers.FirstOrDefault(h => job.Url.StartsWith(h.ProtocolScheme, StringComparison.OrdinalIgnoreCase));
        if (handler == null) 
        {
            job.State = DownloadState.Error;
            job.ErrorMessage = $"No protocol handler found for URL: {job.Url}";
            return;
        }

        try
        {
            // 1. Get File Size
            long fileSize = await handler.GetFileSizeAsync(job.Url, cts.Token);
            job.TotalSize = fileSize;

            // 2. Pre-allocate File on disk (prevents fragmentation and ensures disk space)
            await _fileSystemProvider.PreAllocateFileAsync(job.DestinationFilePath, fileSize);

            // 3. Check Range Requests Support
            bool supportsRange = await handler.SupportsRangeRequestsAsync(job.Url, cts.Token);
            
            // Use configured segments or 1 if range not supported
            int segmentCount = supportsRange && fileSize > 0 ? job.MaxSegments : 1;

            // 4. Divide into segments
            long segmentSize = fileSize / segmentCount;
            for (int i = 0; i < segmentCount; i++)
            {
                long startOffset = i * segmentSize;
                // Ensure the last segment grabs any remaining bytes due to integer division rounding
                long endOffset = (i == segmentCount - 1) ? fileSize - 1 : startOffset + segmentSize - 1;

                var segment = new DownloadSegment
                {
                    JobId = job.Id,
                    StartOffset = startOffset,
                    EndOffset = endOffset,
                    CurrentOffset = startOffset,
                    State = DownloadState.Queued
                };
                job.Segments.Add(segment);
            }

            // 5. Start ISegmentDownloader tasks in parallel
            var downloadTasks = new List<Task>();
            
            foreach (var segment in job.Segments)
            {
                // Open a separate stream handle for each segment to allow concurrent writes
                var stream = _fileSystemProvider.OpenFileForWrite(job.DestinationFilePath);
                
                var task = Task.Run(async () => 
                {
                    segment.State = DownloadState.Downloading;
                    try
                    {
                        await _segmentDownloader.DownloadSegmentAsync(job.Url, segment, handler, stream, cts.Token);
                        if (!cts.Token.IsCancellationRequested)
                        {
                            segment.State = DownloadState.Completed;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Paused or canceled - state is set by the caller
                    }
                    catch (Exception ex)
                    {
                        segment.State = DownloadState.Error;
                        job.ErrorMessage = ex.Message;
                    }
                    finally
                    {
                        await stream.DisposeAsync();
                    }
                }, cts.Token);
                
                downloadTasks.Add(task);
            }

            // Wait for all segments to finish
            try
            {
                await Task.WhenAll(downloadTasks);
            }
            catch (OperationCanceledException) { /* Expected on pause/cancel */ }
            
            if (job.State == DownloadState.Downloading)
            {
                if (job.Segments.All(s => s.IsCompleted))
                {
                    job.State = DownloadState.Completed;
                    job.CompletedAt = DateTime.UtcNow;
                }
                else if (job.Segments.Any(s => s.State == DownloadState.Error))
                {
                    job.State = DownloadState.Error;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Job was paused or canceled
        }
        catch (Exception ex)
        {
            job.State = DownloadState.Error;
            job.ErrorMessage = ex.Message;
        }
        finally
        {
            _jobCancellationTokens.TryRemove(job.Id, out _);
        }
    }

    public Task PauseDownloadAsync(Guid jobId)
    {
        if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
        {
            if (Handlers.StartDownloadCommandHandler.InMemoryJobStore.TryGetValue(jobId, out var job))
            {
                job.State = DownloadState.Paused;
                foreach (var seg in job.Segments.Where(s => s.State == DownloadState.Downloading))
                {
                    seg.State = DownloadState.Paused;
                }
            }
            cts.Cancel();
        }
        return Task.CompletedTask;
    }

    public async Task ResumeDownloadAsync(Guid jobId)
    {
        if (Handlers.StartDownloadCommandHandler.InMemoryJobStore.TryGetValue(jobId, out var job))
        {
            if (job.State == DownloadState.Paused || job.State == DownloadState.Error)
            {
                // Restart the download from where we left off
                job.State = DownloadState.Queued;
                job.Segments.Clear(); // Clear old segments, engine will re-create them
                _ = Task.Run(() => StartDownloadAsync(job, CancellationToken.None));
            }
        }
    }

    public Task CancelDownloadAsync(Guid jobId)
    {
        if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
        {
            if (Handlers.StartDownloadCommandHandler.InMemoryJobStore.TryGetValue(jobId, out var job))
            {
                job.State = DownloadState.Canceled;
                foreach (var seg in job.Segments)
                {
                    seg.State = DownloadState.Canceled;
                }
            }
            cts.Cancel();
        }
        else if (Handlers.StartDownloadCommandHandler.InMemoryJobStore.TryGetValue(jobId, out var job))
        {
            job.State = DownloadState.Canceled;
        }
        return Task.CompletedTask;
    }
}
