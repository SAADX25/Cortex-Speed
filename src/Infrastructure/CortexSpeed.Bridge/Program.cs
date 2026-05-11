// ═══════════════════════════════════════════════
// CortexSpeed.Bridge — Chrome Native Messaging Host
// ═══════════════════════════════════════════════
// This tiny console app is what Chrome actually launches via stdio.
// It reads the Native Messaging payload from stdin, then forwards
// it to the running WPF app via a Named Pipe.
// ═══════════════════════════════════════════════

using System.IO.Pipes;
using System.Text;

const string PipeName = "CortexSpeedDownloadPipe";

try
{
    // 1. Read the 4-byte little-endian length prefix from Chrome
    using var stdin = Console.OpenStandardInput();
    var lengthBytes = new byte[4];
    int bytesRead = stdin.Read(lengthBytes, 0, 4);
    if (bytesRead < 4) return;

    int length = BitConverter.ToInt32(lengthBytes, 0);
    if (length <= 0 || length > 1024 * 1024) return;

    // 2. Read the JSON payload
    var buffer = new byte[length];
    int totalRead = 0;
    while (totalRead < length)
    {
        int read = stdin.Read(buffer, totalRead, length - totalRead);
        if (read == 0) break;
        totalRead += read;
    }

    var json = Encoding.UTF8.GetString(buffer, 0, totalRead);

    // 3. Forward the JSON to the WPF app via Named Pipe
    using var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
    
    // Try to connect with a 3-second timeout
    pipeClient.Connect(3000);

    using var writer = new StreamWriter(pipeClient, Encoding.UTF8) { AutoFlush = true };
    writer.Write(json);

    // 4. Send a success response back to Chrome
    SendResponse(new { status = "ok", message = "Download request forwarded to Cortex Speed" });
}
catch (TimeoutException)
{
    // WPF app is not running
    SendResponse(new { status = "error", message = "Cortex Speed is not running. Please start the application." });
}
catch (Exception ex)
{
    SendResponse(new { status = "error", message = ex.Message });
}

/// <summary>
/// Sends a Native Messaging response back to Chrome (4-byte length prefix + JSON).
/// </summary>
static void SendResponse(object data)
{
    try
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        var lengthBytes = BitConverter.GetBytes(bytes.Length);

        using var stdout = Console.OpenStandardOutput();
        stdout.Write(lengthBytes, 0, 4);
        stdout.Write(bytes, 0, bytes.Length);
        stdout.Flush();
    }
    catch { /* Cannot write to stdout — Chrome may have already disconnected */ }
}
