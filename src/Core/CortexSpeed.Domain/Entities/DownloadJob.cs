using CortexSpeed.Domain.Enums;
using System.Collections.Concurrent;

namespace CortexSpeed.Domain.Entities;

public class DownloadJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string DestinationFilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long TotalSize { get; set; }
    public DownloadState State { get; set; }
    public DownloadCategory Category { get; set; } = DownloadCategory.General;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public int MaxSegments { get; set; } = 16;
    public long SpeedLimitBytesPerSecond { get; set; } = 0; // 0 = unlimited
    public string? ErrorMessage { get; set; }
    
    // Concurrency-safe collection for segments
    public ConcurrentBag<DownloadSegment> Segments { get; set; } = new();

    public long DownloadedSize => Segments.Sum(s => s.DownloadedBytes);
    public double ProgressPercentage => TotalSize == 0 ? 0 : (double)DownloadedSize / TotalSize * 100;
    
    /// <summary>
    /// Auto-classifies the download category based on file extension.
    /// </summary>
    public static DownloadCategory ClassifyByExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".webm" or ".m4v" => DownloadCategory.Videos,
            ".mp3" or ".flac" or ".wav" or ".aac" or ".ogg" or ".wma" or ".m4a" => DownloadCategory.Music,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".webp" or ".ico" => DownloadCategory.Images,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" or ".bz2" or ".xz" or ".iso" => DownloadCategory.Archives,
            ".exe" or ".msi" or ".dmg" or ".deb" or ".rpm" or ".appx" or ".msix" => DownloadCategory.Programs,
            ".pdf" or ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" or ".txt" or ".csv" => DownloadCategory.Documents,
            ".torrent" => DownloadCategory.Torrents,
            _ => DownloadCategory.General
        };
    }
}
