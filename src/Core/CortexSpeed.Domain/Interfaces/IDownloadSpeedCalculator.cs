namespace CortexSpeed.Domain.Interfaces;

public interface IDownloadSpeedCalculator
{
    void ReportBytesDownloaded(long bytes);
    double CurrentSpeedBytesPerSecond { get; }
    TimeSpan GetEstimatedTimeRemaining(long remainingBytes);
}
