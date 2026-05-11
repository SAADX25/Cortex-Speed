using CortexSpeed.Application.Commands;
using CortexSpeed.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace CortexSpeed.Application.Services;

public class NativeMessagingListenerService : BackgroundService
{
    private readonly IBrowserExtensionMessageReceiver _messageReceiver;
    private readonly ISender _mediator;

    public NativeMessagingListenerService(IBrowserExtensionMessageReceiver messageReceiver, ISender mediator)
    {
        _messageReceiver = messageReceiver;
        _mediator = mediator;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to the event fired when the Bridge sends a download request via Named Pipe
        _messageReceiver.OnDownloadRequested += async (sender, args) =>
        {
            try
            {
                var destFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "CortexSpeed");
                if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);
                
                // Use filename from extension if available, otherwise extract from URL
                var fileName = args.FileName;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    try
                    {
                        fileName = Path.GetFileName(new Uri(args.Url).LocalPath);
                    }
                    catch { }
                }
                if (string.IsNullOrWhiteSpace(fileName)) fileName = "downloaded_file.bin";

                // Dispatch the CQRS command to the Download Engine
                var command = new StartDownloadCommand(args.Url, destFolder, fileName);
                await _mediator.Send(command);
            }
            catch
            {
                // Silently ignore routing errors from the pipe
            }
        };

        // Start listening on the Named Pipe
        await _messageReceiver.StartListeningAsync(stoppingToken);
    }
}
