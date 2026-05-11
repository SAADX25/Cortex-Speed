using CortexSpeed.Domain.Interfaces;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace CortexSpeed.Infrastructure.BrowserExtensions;

/// <summary>
/// Listens on a Named Pipe for download requests sent by the CortexSpeed.Bridge console app.
/// The Bridge is the actual Chrome Native Messaging Host (stdio), which reads from Chrome
/// and forwards to this pipe.
/// </summary>
public class NativeMessagingHost : IBrowserExtensionMessageReceiver
{
    public const string PipeName = "CortexSpeedDownloadPipe";

    public event EventHandler<DownloadRequestEventArgs>? OnDownloadRequested;

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Create a new named pipe server and wait for a connection
                using var pipeServer = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await pipeServer.WaitForConnectionAsync(cancellationToken);

                // Read the full message
                using var reader = new StreamReader(pipeServer, Encoding.UTF8);
                var json = await reader.ReadToEndAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        var request = JsonSerializer.Deserialize<DownloadRequestMessage>(
                            json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (request != null && !string.IsNullOrWhiteSpace(request.Url))
                        {
                            OnDownloadRequested?.Invoke(this, new DownloadRequestEventArgs
                            {
                                Url = request.Url,
                                FileName = request.Filename ?? string.Empty,
                                UserAgent = request.UserAgent ?? string.Empty,
                                Cookies = request.Cookies ?? string.Empty
                            });
                        }
                    }
                    catch (JsonException)
                    {
                        // Silently drop malformed messages
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // If pipe fails, wait briefly and retry
                try { await Task.Delay(500, cancellationToken); } catch { break; }
            }
        }
    }

    private class DownloadRequestMessage
    {
        public string? Url { get; set; }
        public string? Filename { get; set; }
        public string? UserAgent { get; set; }
        public string? Cookies { get; set; }
    }
}
