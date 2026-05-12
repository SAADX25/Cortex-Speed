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
    private readonly IDownloadJobRepository _jobRepository;

    // Track CancellationTokenSources per-job for Pause/Cancel support
    private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobCancellationTokens = new();

    public DownloadEngine(
        IEnumerable<IProtocolHandler> protocolHandlers,
        ISegmentDownloader segmentDownloader,
        IFileSystemProvider fileSystemProvider,
        IDownloadJobRepository jobRepository)
    {
        _protocolHandlers = protocolHandlers;
        _segmentDownloader = segmentDownloader;
        _fileSystemProvider = fileSystemProvider;
        _jobRepository = jobRepository;
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

            // 4. Divide into segments only if the job doesn't have segments yet
            if (!job.Segments.Any())
            {
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
            }

            // 5. Start ISegmentDownloader tasks in parallel
            var downloadTasks = new List<Task>();
            
            // ToList is used because job.Segments might be modified during segment stealing
            foreach (var segment in job.Segments.ToList())
            {
                if (segment.IsCompleted || segment.CurrentOffset > segment.EndOffset)
                {
                    segment.State = DownloadState.Completed;
                    continue;
                }

                downloadTasks.Add(RunSegmentWorkerAsync(job, segment, handler, cts.Token));
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

    public async Task PauseDownloadAsync(Guid jobId)
    {
        if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job != null)
            {
                job.State = DownloadState.Paused;
                foreach (var seg in job.Segments.Where(s => s.State == DownloadState.Downloading))
                {
                    seg.State = DownloadState.Paused;
                }
            }
            cts.Cancel();
        }
    }

    public async Task ResumeDownloadAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        if (job != null)
        {
            if (job.State == DownloadState.Paused || job.State == DownloadState.Error)
            {
                // Create a new CancellationTokenSource for the resumed download
                var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken.None);
                _jobCancellationTokens[job.Id] = cts;
                
                job.State = DownloadState.Downloading;

                // Find the protocol handler
                var handler = _protocolHandlers.FirstOrDefault(h => job.Url.StartsWith(h.ProtocolScheme, StringComparison.OrdinalIgnoreCase));
                if (handler == null)
                {
                    job.State = DownloadState.Error;
                    job.ErrorMessage = $"No protocol handler found for URL: {job.Url}";
                    return;
                }

                // Only create new download tasks for segments that are paused or in error state
                // Segments that were already completed are left as-is
                var downloadTasks = new List<Task>();
                
                foreach (var segment in job.Segments.Where(s => s.State == DownloadState.Paused || s.State == DownloadState.Error).ToList())
                {
                    downloadTasks.Add(RunSegmentWorkerAsync(job, segment, handler, cts.Token));
                }

                // Wait for all resumed segments to finish
                _ = Task.Run(async () =>
                {
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

                    _jobCancellationTokens.TryRemove(job.Id, out _);
                });
            }
        }
    }

    public async Task CancelDownloadAsync(Guid jobId)
    {
        if (_jobCancellationTokens.TryGetValue(jobId, out var cts))
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job != null)
            {
                job.State = DownloadState.Canceled;
                foreach (var seg in job.Segments)
                {
                    seg.State = DownloadState.Canceled;
                }
            }
            cts.Cancel();
        }
        else 
        {
            var job = await _jobRepository.GetByIdAsync(jobId);
            if (job != null)
            {
                job.State = DownloadState.Canceled;
            }
        }
    }

    private Task RunSegmentWorkerAsync(DownloadJob job, DownloadSegment initialSegment, IProtocolHandler handler, CancellationToken token)
    {
        return Task.Run(async () =>
        {
            var currentSegment = initialSegment;

            while (currentSegment != null && !token.IsCancellationRequested)
            {
                var stream = _fileSystemProvider.OpenFileForWrite(job.DestinationFilePath);
                try
                {
                    currentSegment.State = DownloadState.Downloading;
                    await _segmentDownloader.DownloadSegmentAsync(job.Url, currentSegment, handler, stream, token);
                    
                    if (!token.IsCancellationRequested)
                    {
                        currentSegment.State = DownloadState.Completed;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Paused or canceled
                }
                catch (Exception ex)
                {
                    currentSegment.State = DownloadState.Error;
                    job.ErrorMessage = ex.Message;
                }
                finally
                {
                    await stream.DisposeAsync();
                }

                if (token.IsCancellationRequested || job.State != DownloadState.Downloading)
                {
                    break;
                }

                currentSegment = null; // Assume no more work

                // Dynamic Segmentation / Segment Stealing
                lock (job.Segments)
                {
                    var largestSegment = job.Segments
                        .Where(s => s.State == DownloadState.Downloading || s.State == DownloadState.Queued)
                        .OrderByDescending(s => s.EndOffset - s.CurrentOffset)
                        .FirstOrDefault();

                    // Only steal if remaining size is significant (e.g., > 1MB) to prevent tiny redundant splits
                    if (largestSegment != null)
                    {
                        long remaining = largestSegment.EndOffset - largestSegment.CurrentOffset;
                        if (remaining > 1024 * 1024)
                        {
                            long splitOffset = largestSegment.CurrentOffset + (remaining / 2);
                            
                            var newSegment = new DownloadSegment
                            {
                                JobId = job.Id,
                                StartOffset = splitOffset + 1,
                                EndOffset = largestSegment.EndOffset,
                                CurrentOffset = splitOffset + 1,
                                State = DownloadState.Queued
                            };
                            
                            largestSegment.EndOffset = splitOffset;
                            job.Segments.Add(newSegment);
                            
                            currentSegment = newSegment;
                        }
                    }
                }
            }
        }, token);
    }
}
