using CortexSpeed.Domain.Interfaces;
using System.Net.Http.Headers;

namespace CortexSpeed.Infrastructure.Network;

public class HttpProtocolHandler : IProtocolHandler
{
    private readonly HttpClient _httpClient;

    public HttpProtocolHandler(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string ProtocolScheme => "http";

    public async Task<long> GetFileSizeAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            response.EnsureSuccessStatusCode();
            
            return response.Content.Headers.ContentLength ?? 0;
        }
        catch (HttpRequestException)
        {
            // Fallback for servers that block HEAD requests
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
            getRequest.Headers.Range = new RangeHeaderValue(0, 0);
            using var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            
            getResponse.EnsureSuccessStatusCode();
            
            if (getResponse.Content.Headers.ContentRange?.Length != null)
            {
                return getResponse.Content.Headers.ContentRange.Length.Value;
            }
            
            return getResponse.Content.Headers.ContentLength ?? 0;
        }
    }

    public async Task<bool> SupportsRangeRequestsAsync(string url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, url);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        return response.Headers.AcceptRanges.Contains("bytes");
    }

    public async Task<Stream> GetStreamAsync(string url, long startOffset, long endOffset, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(startOffset, endOffset);
        
        // HttpCompletionOption.ResponseHeadersRead is CRITICAL for high performance streaming.
        // It ensures the stream is returned before the entire body is buffered in memory.
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            throw new HttpRequestException($"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).");
        }
        
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}
