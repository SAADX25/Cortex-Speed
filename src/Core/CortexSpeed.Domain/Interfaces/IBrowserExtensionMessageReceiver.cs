namespace CortexSpeed.Domain.Interfaces;

/// <summary>
/// Contract for receiving download requests from browser extensions (e.g., Native Messaging).
/// </summary>
public interface IBrowserExtensionMessageReceiver
{
    event EventHandler<DownloadRequestEventArgs> OnDownloadRequested;
    Task StartListeningAsync(CancellationToken cancellationToken);
}

public class DownloadRequestEventArgs : EventArgs
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Cookies { get; set; } = string.Empty;
}
