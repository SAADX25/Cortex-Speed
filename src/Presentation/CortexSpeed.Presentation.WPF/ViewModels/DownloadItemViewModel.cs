using CommunityToolkit.Mvvm.ComponentModel;
using CortexSpeed.Domain.Enums;
using System;

namespace CortexSpeed.Presentation.WPF.ViewModels;

public partial class DownloadItemViewModel : ObservableObject
{
    public Guid JobId { get; set; }
    
    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private string _speed = "—";

    [ObservableProperty]
    private DownloadState _status;

    [ObservableProperty]
    private DownloadCategory _category = DownloadCategory.General;

    [ObservableProperty]
    private long _totalSize;

    [ObservableProperty]
    private long _downloadedSize;

    [ObservableProperty]
    private string _eta = "—";

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private DateTime _createdAt;

    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Whether this item is currently selected in the list.
    /// </summary>
    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Human-readable total size string.
    /// </summary>
    public string TotalSizeFormatted => FormatBytes(TotalSize);

    /// <summary>
    /// Human-readable downloaded size string.
    /// </summary>
    public string DownloadedSizeFormatted => FormatBytes(DownloadedSize);

    /// <summary>
    /// Progress text like "45.2%"
    /// </summary>
    public string ProgressText => ProgressPercentage > 0 ? $"{ProgressPercentage:F1}%" : "—";

    partial void OnProgressPercentageChanged(double value)
    {
        OnPropertyChanged(nameof(ProgressText));
    }

    partial void OnTotalSizeChanged(long value)
    {
        OnPropertyChanged(nameof(TotalSizeFormatted));
    }

    partial void OnDownloadedSizeChanged(long value)
    {
        OnPropertyChanged(nameof(DownloadedSizeFormatted));
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes <= 0) return "—";
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
}
