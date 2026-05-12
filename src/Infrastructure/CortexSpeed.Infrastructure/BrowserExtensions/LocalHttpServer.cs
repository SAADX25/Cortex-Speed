using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

namespace CortexSpeed.Infrastructure.BrowserExtensions;

/// <summary>
/// Lightweight HTTP server on localhost:19256
/// Receives download requests from the Chrome extension.
/// GET  /ping           → health check (extension uses this to check app is running)
/// POST /download       → JSON body: { url, filename }
/// GET  /download?url=  → quick URL download
/// </summary>
public class LocalHttpServer : BackgroundService
{
    private readonly ILogger<LocalHttpServer> _logger;

    public const int Port = 19256;

    /// <summary>
    /// Set this from the WPF layer to open the "Add Download" dialog
    /// instead of silently starting a download.
    /// Signature: (url, suggestedFilename)
    /// </summary>
    public static Action<string, string>? ShowDownloadDialog { get; set; }

    public LocalHttpServer(ILogger<LocalHttpServer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{Port}/");

        try
        {
            listener.Start();
            _logger.LogInformation("[CortexSpeed] HTTP server listening on http://localhost:{Port}/", Port);

            while (!stoppingToken.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync().WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch { continue; }

                // Handle each request in background (don't block the listener loop)
                _ = Task.Run(() => HandleRequest(ctx), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CortexSpeed] HTTP server failed to start");
        }
        finally
        {
            try { listener.Stop(); } catch { }
        }
    }

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        var req  = ctx.Request;
        var resp = ctx.Response;

        // Determine the Origin of the request
        var origin = req.Headers["Origin"];

        // Replace this with your actual installed Extension ID.
        // For example: "chrome-extension://pabafhkpefkiponmjecfomfljdobgipm"
        const string AllowedExtensionId = "chrome-extension://<your-extension-id>";

        // Standard CSRF / RCE mitigation: ONLY allow requests from the designated extension origin
        bool originAllowed = !string.IsNullOrEmpty(origin) &&
                             (origin == AllowedExtensionId || origin.StartsWith("chrome-extension://"));

        // CORS — allow Chrome extension to call us
        if (originAllowed)
        {
            resp.Headers.Add("Access-Control-Allow-Origin", origin);
        }
        else
        {
            resp.Headers.Add("Access-Control-Allow-Origin", AllowedExtensionId);
        }
        
        resp.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        resp.ContentType = "application/json";

        try
        {
            // Handle OPTIONS preflight
            if (req.HttpMethod == "OPTIONS")
            {
                resp.StatusCode = 204;
                resp.Close();
                return;
            }

            // Reject if origin is invalid to prevent CSRF from malicious websites
            if (!originAllowed && req.HttpMethod != "GET")
            {
                await WriteJson(resp, 403, new { status = "error", message = "Forbidden - Invalid Origin." });
                return;
            }

            string path = req.Url?.AbsolutePath ?? "/";

            // ── GET /ping ──────────────────────────────────────
            if (path == "/ping")
            {
                await WriteJson(resp, 200, new { status = "ok", app = "CortexSpeed", version = "2.0" });
                return;
            }

            // ── POST /download  or  GET /download?url=... ──────
            if (path == "/download")
            {
                string? url = null;
                string? filename = null;

                if (req.HttpMethod == "POST")
                {
                    using var reader = new System.IO.StreamReader(req.InputStream, Encoding.UTF8);
                    var body = await reader.ReadToEndAsync();
                    var data = JsonSerializer.Deserialize<DownloadRequest>(body,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    url      = data?.Url;
                    filename = data?.Filename;
                }
                else // GET
                {
                    url      = HttpUtility.UrlDecode(req.QueryString["url"] ?? "");
                    filename = HttpUtility.UrlDecode(req.QueryString["filename"] ?? "");
                }

                if (string.IsNullOrWhiteSpace(url))
                {
                    await WriteJson(resp, 400, new { status = "error", message = "url is required" });
                    return;
                }

                // Resolve suggested filename from URL (may be overridden by user in dialog)
                if (string.IsNullOrWhiteSpace(filename))
                {
                    try { filename = System.IO.Path.GetFileName(new Uri(url).LocalPath); } catch { }
                    if (string.IsNullOrWhiteSpace(filename)) filename = $"download_{DateTime.Now:yyyyMMdd_HHmmss}.bin";
                }

                _logger.LogInformation("[CortexSpeed] Browser download intercepted: {Url} → showing dialog", url);

                // ── Open the UI dialog instead of auto-starting ──
                if (ShowDownloadDialog != null)
                {
                    var capturedUrl = url;
                    var capturedFile = filename;
                    ShowDownloadDialog.Invoke(capturedUrl, capturedFile);
                    await WriteJson(resp, 200, new { status = "ok", message = "dialog_opened", filename });
                }
                else
                {
                    // Fallback: dialog not wired yet
                    await WriteJson(resp, 503, new { status = "error", message = "app_not_ready" });
                }
                return;
            }

            // Unknown route
            await WriteJson(resp, 404, new { status = "error", message = "not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CortexSpeed] Request error");
            try { await WriteJson(resp, 500, new { status = "error", message = ex.Message }); } catch { }
        }
    }

    private static async Task WriteJson(HttpListenerResponse resp, int statusCode, object data)
    {
        resp.StatusCode = statusCode;
        var json = JsonSerializer.Serialize(data);
        var bytes = Encoding.UTF8.GetBytes(json);
        resp.ContentLength64 = bytes.Length;
        await resp.OutputStream.WriteAsync(bytes);
        resp.Close();
    }

    private class DownloadRequest
    {
        public string? Url { get; set; }
        public string? Filename { get; set; }
    }
}
