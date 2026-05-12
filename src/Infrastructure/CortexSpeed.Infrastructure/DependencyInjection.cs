using CortexSpeed.Domain.Interfaces;
using CortexSpeed.Infrastructure.BrowserExtensions;
using CortexSpeed.Infrastructure.FileSystem;
using CortexSpeed.Infrastructure.Network;
using CortexSpeed.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http;

namespace CortexSpeed.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Persistence
        services.AddSingleton<IDownloadJobRepository, JsonDownloadJobRepository>();

        // Register the high-performance disk I/O provider
        services.AddSingleton<IFileSystemProvider, FileSystemProvider>();

        // Register the network segment downloader
        services.AddSingleton<ISegmentDownloader, SegmentDownloader>();

        // Register the local HTTP server that receives download requests from Chrome extension
        // Extension POSTs to http://localhost:19256/download — simple and reliable
        services.AddHostedService<LocalHttpServer>();

        // Advanced HttpClient configuration for high-performance downloading
        services.AddHttpClient<IProtocolHandler, HttpProtocolHandler>(client =>
        {
            // Default timeout handles stalled connections rather than letting them hang forever
            client.Timeout = TimeSpan.FromMinutes(5); 
            // Set a custom user agent to prevent being blocked by some CDNs or firewalls
            client.DefaultRequestHeaders.UserAgent.ParseAdd("CortexSpeed/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            // Maximizes concurrent connections to a single server (crucial for multi-segment downloading)
            MaxConnectionsPerServer = 100, 
            
            // Pooled connections help avoid TCP port exhaustion while still refreshing DNS
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromSeconds(30),
            
            // Enable compression for metadata requests (HEAD) to save bandwidth
            AutomaticDecompression = DecompressionMethods.All,

            // Advanced TCP options for download throughput (multiplexing)
            EnableMultipleHttp2Connections = true
        });

        return services;
    }
}
