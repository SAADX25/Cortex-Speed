using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CortexSpeed.Application.Commands;
using MediatR;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using CortexSpeed.Presentation.WPF.ViewModels;

namespace CortexSpeed.Presentation.WPF.ViewModels;

public partial class AddDownloadViewModel : ObservableObject
{
    private readonly ISender _mediator;
    private readonly MainViewModel _mainViewModel;
    private readonly SettingsViewModel _settingsViewModel;

    private static readonly HttpClient _httpProbe = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10
    })
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _savePath = string.Empty;

    [ObservableProperty]
    private int _segmentCount = 16;

    [ObservableProperty]
    private bool _scheduleEnabled;

    [ObservableProperty]
    private DateTime _scheduleDate = DateTime.Now.AddHours(1);

    public AddDownloadViewModel(ISender mediator, MainViewModel mainViewModel, SettingsViewModel settingsViewModel)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
    }

    [RelayCommand]
    private void Open()
    {
        Url = string.Empty;
        FileName = string.Empty;
        SavePath = _settingsViewModel.DefaultDownloadFolder;
        SegmentCount = 16;
        ScheduleEnabled = false;
        ScheduleDate = DateTime.Now.AddHours(1);
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    [RelayCommand]
    private void SelectSaveFolder()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Download Folder",
            InitialDirectory = SavePath
        };
        if (dialog.ShowDialog() == true)
        {
            SavePath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private async Task ConfirmAddDownloadAsync()
    {
        var targetUrl = string.IsNullOrWhiteSpace(Url) ? _mainViewModel.UrlInput : Url;
        if (string.IsNullOrWhiteSpace(targetUrl)) return;

        // Ensure URL has a scheme
        if (!targetUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !targetUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            !targetUrl.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase))
        {
            targetUrl = "https://" + targetUrl;
        }

        var targetFileName = FileName;
        if (string.IsNullOrWhiteSpace(targetFileName))
        {
            targetFileName = _mainViewModel.ExtractFileName(targetUrl);
        }

        var destFolder = string.IsNullOrWhiteSpace(SavePath) ? _settingsViewModel.DefaultDownloadFolder : SavePath;
        Directory.CreateDirectory(destFolder);

        DateTime? scheduledAt = ScheduleEnabled ? ScheduleDate : null;

        var command = new StartDownloadCommand(targetUrl, destFolder, targetFileName, SegmentCount, 0, scheduledAt);
        var jobId = await _mediator.Send(command);

        var category = Domain.Entities.DownloadJob.ClassifyByExtension(targetFileName);

        _mainViewModel.AllDownloads.Add(new DownloadItemViewModel
        {
            JobId = jobId,
            FileName = targetFileName,
            Status = scheduledAt.HasValue ? Domain.Enums.DownloadState.Scheduled : Domain.Enums.DownloadState.Queued,
            ProgressPercentage = 0,
            Category = category,
            Url = targetUrl,
            DestinationPath = Path.Combine(destFolder, targetFileName),
            CreatedAt = DateTime.UtcNow
        });

        // Switch to "All" so the new download is visible
        _mainViewModel.SelectedCategory = "All";
        _mainViewModel.UrlInput = string.Empty;
        IsOpen = false;
    }

    public void OpenFromBrowser(string url, string suggestedFilename)
    {
        Task.Run(async () =>
        {
            var resolvedName = await ResolveFileNameAsync(url, suggestedFilename);

            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                Url = url;
                FileName = resolvedName;
                SavePath = _settingsViewModel.DefaultDownloadFolder;
                SegmentCount = 16;
                ScheduleEnabled = false;
                ScheduleDate = DateTime.Now.AddHours(1);
                IsOpen = true;

                var mainWindow = System.Windows.Application.Current?.MainWindow;
                if (mainWindow != null)
                {
                    if (mainWindow.WindowState == WindowState.Minimized)
                        mainWindow.WindowState = WindowState.Normal;
                    mainWindow.Activate();
                    mainWindow.Topmost = true;
                    mainWindow.Topmost = false;
                    mainWindow.Focus();
                }
            });
        });
    }

    private static async Task<string> ResolveFileNameAsync(string url, string fallback)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 CortexSpeed/4.0");
            using var resp = await _httpProbe.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

            // 1. Try Content-Disposition header
            if (resp.Content.Headers.ContentDisposition is { } cd)
            {
                var name = cd.FileNameStar ?? cd.FileName;
                if (!string.IsNullOrWhiteSpace(name))
                {
                    name = name.Trim('"', ' ', '\'');
                    name = SanitizeFileName(name);
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }

            // 2. Try the final (redirected) URL path
            var finalUrl = resp.RequestMessage?.RequestUri?.ToString() ?? url;
            var fromPath = Path.GetFileName(new Uri(finalUrl).LocalPath);
            fromPath = SanitizeFileName(fromPath);
            if (!string.IsNullOrWhiteSpace(fromPath) && fromPath.Length > 3 && fromPath.Contains('.'))
                return fromPath;
        }
        catch { /* Network error — use fallback */ }

        // 3. Fallback: try the original URL path
        try
        {
            var fromOrig = Path.GetFileName(new Uri(url).LocalPath);
            fromOrig = SanitizeFileName(fromOrig);
            if (!string.IsNullOrWhiteSpace(fromOrig) && fromOrig.Length > 3 && fromOrig.Contains('.'))
                return fromOrig;
        }
        catch { }

        return string.IsNullOrWhiteSpace(fallback) ? $"download_{DateTime.Now:yyyyMMdd_HHmmss}.bin" : fallback;
    }

    private static string SanitizeFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        try { name = Uri.UnescapeDataString(name); } catch { }
        var q = name.IndexOf('?');
        if (q >= 0) name = name[..q];
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Trim();
    }
}