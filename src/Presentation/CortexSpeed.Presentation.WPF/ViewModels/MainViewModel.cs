using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Enums;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CortexSpeed.Application.Handlers;

namespace CortexSpeed.Presentation.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISender _mediator;
    private readonly CortexSpeed.Domain.Interfaces.IDownloadJobRepository _jobRepository;

    // Used to resolve real filenames from Content-Disposition headers
    private static readonly HttpClient _httpProbe = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10
    })
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    // ──────────────────────────────────────
    // URL Input
    // ──────────────────────────────────────
    [ObservableProperty]
    private string _urlInput = string.Empty;

    // ──────────────────────────────────────
    // Child ViewModels
    // ──────────────────────────────────────
    public SettingsViewModel Settings { get; }
    public AddDownloadViewModel AddDownload { get; }

    // ──────────────────────────────────────
    // Navigation
    // ──────────────────────────────────────
    [ObservableProperty]
    private string _selectedCategory = "All";

    // ──────────────────────────────────────
    // Statistics
    // ──────────────────────────────────────
    [ObservableProperty]
    private string _totalDownloaded = "0 B";

    [ObservableProperty]
    private string _activeCount = "0";

    [ObservableProperty]
    private string _completedCount = "0";

    [ObservableProperty]
    private string _currentTotalSpeed = "0 MB/s";

    // ──────────────────────────────────────
    // Collections
    // ──────────────────────────────────────
    public ObservableCollection<DownloadItemViewModel> AllDownloads { get; } = new();

    /// <summary>
    /// Filtered view based on the selected sidebar category.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DownloadItemViewModel> _filteredDownloads = new();

    /// <summary>
    /// Sidebar navigation items.
    /// </summary>
    public ObservableCollection<string> SidebarItems { get; } = new()
    {
        "All",
        "Active",
        "Completed",
        "Scheduled",
        "Errors",
        "───",
        "Videos",
        "Music",
        "Documents",
        "Archives",
        "Programs",
        "Images"
    };

    // ──────────────────────────────────────
    // Selected Item
    // ──────────────────────────────────────
    [ObservableProperty]
    private DownloadItemViewModel? _selectedDownload;

    // Speed tracking
    private readonly Dictionary<Guid, long> _previousBytes = new();
    private DateTime _lastSpeedCheck = DateTime.UtcNow;

    public MainViewModel(ISender mediator, CortexSpeed.Domain.Interfaces.IDownloadJobRepository jobRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
        
        Settings = new SettingsViewModel();
        AddDownload = new AddDownloadViewModel(_mediator, this, Settings);

        // Start a lightweight timer to poll download progress and update UI
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        timer.Tick += (s, e) => UpdateProgress();
        timer.Start();

        // Initialize clipboard monitor
        StartClipboardMonitor();
    }

    private async void UpdateProgress()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastSpeedCheck).TotalSeconds;
        double totalSpeed = 0;
        long totalAllDownloaded = 0;

        // Separate active and inactive downloads to avoid recalculating metrics for completed/paused downloads
        var activeDownloads = AllDownloads.Where(d => d.Status == DownloadState.Downloading || d.Status == DownloadState.Queued).ToList();
        var inactiveDownloads = AllDownloads.Where(d => d.Status != DownloadState.Downloading && d.Status != DownloadState.Queued).ToList();

        // ═══ ACTIVE DOWNLOADS: Full calculation ═══
        foreach (var item in activeDownloads)
        {
            var job = await _jobRepository.GetByIdAsync(item.JobId);
            if (item.JobId != Guid.Empty && job != null)
            {
                // Update basic status
                item.Status = job.State;
                item.ErrorMessage = job.ErrorMessage;

                // Calculate total downloaded bytes across all segments (only for active downloads)
                long downloadedBytes = job.Segments.Sum(s => s.DownloadedBytes);
                item.DownloadedSize = downloadedBytes;
                item.TotalSize = job.TotalSize;
                totalAllDownloaded += downloadedBytes;

                if (job.TotalSize > 0)
                {
                    item.ProgressPercentage = (double)downloadedBytes / job.TotalSize * 100;
                }

                // Speed calculation with delta tracking (only for Downloading state)
                if (elapsed > 0.1 && job.State == DownloadState.Downloading)
                {
                    if (_previousBytes.TryGetValue(item.JobId, out var prevBytes))
                    {
                        long delta = downloadedBytes - prevBytes;
                        double speedBps = delta / elapsed;
                        totalSpeed += speedBps;
                        
                        if (speedBps > 0)
                        {
                            item.Speed = FormatSpeed(speedBps);
                            
                            // ETA calculation
                            long remaining = job.TotalSize - downloadedBytes;
                            if (remaining > 0 && speedBps > 0)
                            {
                                var eta = TimeSpan.FromSeconds(remaining / speedBps);
                                item.Eta = FormatTimeSpan(eta);
                            }
                            else
                            {
                                item.Eta = "—";
                            }
                        }
                    }
                    _previousBytes[item.JobId] = downloadedBytes;
                }
            }
        }

        // ═══ INACTIVE DOWNLOADS: Minimal updates ═══
        foreach (var item in inactiveDownloads)
        {
            var job = await _jobRepository.GetByIdAsync(item.JobId);
            if (item.JobId != Guid.Empty && job != null)
            {
                // Only update status and error message (cheap operation)
                item.Status = job.State;
                item.ErrorMessage = job.ErrorMessage;

                // For paused/completed/error/canceled, clear speed and ETA (don't recalculate segments)
                if (item.Speed != "—")
                {
                    item.Speed = "—";
                    item.Eta = "—";
                }

                // Include completed download sizes in total (they won't change)
                if (job.State == DownloadState.Completed && item.TotalSize > 0)
                {
                    totalAllDownloaded += item.TotalSize;
                }
            }
        }

        if (elapsed > 0.1)
        {
            _lastSpeedCheck = now;
        }

        // Update statistics
        CurrentTotalSpeed = FormatSpeed(totalSpeed);
        ActiveCount = AllDownloads.Count(d => d.Status == DownloadState.Downloading).ToString();
        CompletedCount = AllDownloads.Count(d => d.Status == DownloadState.Completed).ToString();
        TotalDownloaded = FormatBytes(totalAllDownloaded);

        // Re-filter if needed
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = SelectedCategory switch
        {
            "All" => AllDownloads.ToList(),
            "Active" => AllDownloads.Where(d => d.Status == DownloadState.Downloading || d.Status == DownloadState.Queued).ToList(),
            "Completed" => AllDownloads.Where(d => d.Status == DownloadState.Completed).ToList(),
            "Scheduled" => AllDownloads.Where(d => d.Status == DownloadState.Scheduled).ToList(),
            "Errors" => AllDownloads.Where(d => d.Status == DownloadState.Error || d.Status == DownloadState.Canceled).ToList(),
            "Videos" => AllDownloads.Where(d => d.Category == DownloadCategory.Videos).ToList(),
            "Music" => AllDownloads.Where(d => d.Category == DownloadCategory.Music).ToList(),
            "Documents" => AllDownloads.Where(d => d.Category == DownloadCategory.Documents).ToList(),
            "Archives" => AllDownloads.Where(d => d.Category == DownloadCategory.Archives).ToList(),
            "Programs" => AllDownloads.Where(d => d.Category == DownloadCategory.Programs).ToList(),
            "Images" => AllDownloads.Where(d => d.Category == DownloadCategory.Images).ToList(),
            _ => AllDownloads.ToList()
        };

        // Only update if the list has actually changed
        if (FilteredDownloads.Count != filtered.Count || !filtered.SequenceEqual(FilteredDownloads))
        {
            FilteredDownloads = new ObservableCollection<DownloadItemViewModel>(filtered);
        }
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        ApplyFilter();
    }

    // ──────────────────────────────────────
    // Commands
    // ──────────────────────────────────────

    public void OpenAddDialogFromBrowser(string url, string suggestedFilename)
    {
        AddDownload.OpenFromBrowser(url, suggestedFilename);
    }

    [RelayCommand]
    private async Task QuickDownloadAsync()
    {
    {
        if (string.IsNullOrWhiteSpace(UrlInput)) return;

        var url = UrlInput.Trim();

        // Ensure URL has a scheme
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        var fileName = ExtractFileName(url);

        Directory.CreateDirectory(Settings.DefaultDownloadFolder);

        try
        {
            var command = new StartDownloadCommand(url, Settings.DefaultDownloadFolder, fileName);
            var jobId = await _mediator.Send(command);

            var category = Domain.Entities.DownloadJob.ClassifyByExtension(fileName);

            AllDownloads.Add(new DownloadItemViewModel
            {
                JobId = jobId,
                FileName = fileName,
                Status = DownloadState.Queued,
                ProgressPercentage = 0,
                Category = category,
                Url = url,
                DestinationPath = Path.Combine(Settings.DefaultDownloadFolder, fileName),
                CreatedAt = DateTime.UtcNow
            });

            // Switch to "All" so the new download is visible regardless of current filter
            SelectedCategory = "All";
            UrlInput = string.Empty;
        }
        catch (Exception ex)
        {
            // Show error in UI - add a failed item
            AllDownloads.Add(new DownloadItemViewModel
            {
                FileName = fileName,
                Status = DownloadState.Error,
                ErrorMessage = ex.Message,
                Url = url,
                CreatedAt = DateTime.UtcNow
            });
            SelectedCategory = "All";
            UrlInput = string.Empty;
        }
    }

    [RelayCommand]
    private async Task PauseDownloadAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;
        await _mediator.Send(new PauseDownloadCommand(item.JobId));
    }

    [RelayCommand]
    private async Task ResumeDownloadAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;
        await _mediator.Send(new ResumeDownloadCommand(item.JobId));
    }

    [RelayCommand]
    private async Task CancelDownloadAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;
        await _mediator.Send(new CancelDownloadCommand(item.JobId));
    }

    [RelayCommand]
    private async Task RemoveDownloadAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;
        await _mediator.Send(new RemoveDownloadCommand(item.JobId, false));
        AllDownloads.Remove(item);
    }

    [RelayCommand]
    private async Task RemoveWithFileAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;
        await _mediator.Send(new RemoveDownloadCommand(item.JobId, true));
        AllDownloads.Remove(item);
    }

    [RelayCommand]
    private void OpenFile(DownloadItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.DestinationPath)) return;
        if (File.Exists(item.DestinationPath))
        {
            try { Process.Start(new ProcessStartInfo(item.DestinationPath) { UseShellExecute = true }); } catch { }
        }
    }

    [RelayCommand]
    private void OpenFolder(DownloadItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.DestinationPath)) return;
        var folder = Path.GetDirectoryName(item.DestinationPath);
        if (folder != null && Directory.Exists(folder))
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{item.DestinationPath}\"") { UseShellExecute = true }); } catch { }
        }
    }

    [RelayCommand]
    private void CopyUrl(DownloadItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Url)) return;
        try { Clipboard.SetText(item.Url); } catch { }
    }

    [RelayCommand]
    private async Task RetryDownloadAsync(DownloadItemViewModel? item)
    {
        if (item == null) return;
        await _mediator.Send(new ResumeDownloadCommand(item.JobId));
    }

    [RelayCommand]
    private async Task PauseAllAsync()
    {
        foreach (var item in AllDownloads.Where(d => d.Status == DownloadState.Downloading).ToList())
        {
            await _mediator.Send(new PauseDownloadCommand(item.JobId));
        }
    }

    [RelayCommand]
    private async Task ResumeAllAsync()
    {
        foreach (var item in AllDownloads.Where(d => d.Status == DownloadState.Paused).ToList())
        {
            await _mediator.Send(new ResumeDownloadCommand(item.JobId));
        }
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        var completed = AllDownloads.Where(d => d.Status == DownloadState.Completed).ToList();
        foreach (var item in completed)
        {
            AllDownloads.Remove(item);
            _jobRepository.RemoveAsync(item.JobId).Wait();
        }
    }

    // ──────────────────────────────────────
    // Clipboard Monitor
    // ──────────────────────────────────────
    private string _lastClipboardUrl = string.Empty;

    private void StartClipboardMonitor()
    {
        var clipTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        clipTimer.Tick += (s, e) =>
        {
            if (!Settings.ClipboardMonitorEnabled) return;
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText().Trim();
                    if (text != _lastClipboardUrl && 
                        (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                         text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                         text.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase)))
                    {
                        _lastClipboardUrl = text;
                        UrlInput = text;
                    }
                }
            }
            catch { /* Clipboard might be locked */ }
        };
        clipTimer.Start();
    }

    // ──────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────
    
    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond <= 0) return "—";
        if (bytesPerSecond >= 1024 * 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
        if (bytesPerSecond >= 1024 * 1024) return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        if (bytesPerSecond >= 1024) return $"{bytesPerSecond / 1024:F1} KB/s";
        return $"{bytesPerSecond:F0} B/s";
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalDays >= 1) return $"{ts.Days}d {ts.Hours}h";
        if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m";
        if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
        return $"{ts.Seconds}s";
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Safely extracts a filename from a URL. Handles invalid URIs, query strings, 
    /// and URLs without file extensions.
    /// </summary>
    public static string ExtractFileName(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.LocalPath;
            var fileName = Path.GetFileName(path);
            
            // Clean invalid file name characters
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }
            }

            // If still empty or has no extension, generate a reasonable name
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "/" || fileName.Length < 2)
            {
                fileName = $"download_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
            }

            return fileName;
        }
        catch
        {
            return $"download_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
        }
    }
}
