using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IProxyService, ProxyService>();

var app = builder.Build();

// Health check endpoint
app.MapGet("/health", () => Results.Ok());

// Main proxy handler
app.MapFallback(async (HttpContext context, IProxyService proxyService) =>
{
    await proxyService.HandleProxyRequest(context);
});

app.Run();

public interface IProxyService
{
    Task HandleProxyRequest(HttpContext context);
}

public class ProxyService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    
    private readonly string _moviesServiceUrl;
    private readonly string _monolithUrl;
    private readonly bool _migrationEnabled;
    private readonly int _migrationPercent;

    public ProxyService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        _moviesServiceUrl = _configuration["MOVIES_SERVICE_URL"] ?? "";
        _monolithUrl = _configuration["MONOLITH_URL"] ?? "";
        _migrationEnabled = _configuration["GRADUAL_MIGRATION"] == "true";
        _migrationPercent = int.TryParse(_configuration["MOVIES_MIGRATION_PERCENT"], out var percent) ? percent : 0;
    }

    public async Task HandleProxyRequest(HttpContext context)
    {
        var path = context.Request.Path.Value;
        string targetUrl;

        // Routes that always go to monolith
        if (path == "/api/users" || path == "/api/payments" || path == "/api/subscriptions")
        {
            targetUrl = _monolithUrl;
        }
        else if (path == "/api/movies" || path == "/api/movies/health")
        {
            // Migration logic for movies microservice
            targetUrl = ShouldUseMicroservice(context) ? _moviesServiceUrl : _monolithUrl;
        }
        else if (path == "/health")
        {
            // Health check is handled separately, this shouldn't be reached
            context.Response.StatusCode = 200;
            return;
        }
        else
        {
            // Default: proxy to monolith
            targetUrl = _monolithUrl;
        }

        if (string.IsNullOrEmpty(targetUrl))
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Target URL not configured");
            return;
        }

        await ProxyRequest(context, targetUrl);
    }

    private bool ShouldUseMicroservice(HttpContext context)
    {
        if (!_migrationEnabled)
            return false;

        var userId = context.Request.Headers["X-User-ID"].FirstOrDefault() 
                    ?? context.Connection.RemoteIpAddress?.ToString() 
                    ?? "";

        var percent = HashToPercent(userId);
        return percent < _migrationPercent;
    }

    private static int HashToPercent(string userId)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(userId));
        var value = BitConverter.ToUInt16(hash, 0);
        return value % 100;
    }

    private async Task ProxyRequest(HttpContext context, string targetBaseUrl)
    {
        var request = context.Request;
        var targetUrl = $"{targetBaseUrl.TrimEnd('/')}{request.Path}{request.QueryString}";

        try
        {
            using var requestMessage = new HttpRequestMessage();
            requestMessage.Method = new HttpMethod(request.Method);
            requestMessage.RequestUri = new Uri(targetUrl);

            // Copy headers
            foreach (var header in request.Headers)
            {
                if (!header.Key.StartsWith(":") && 
                    header.Key != "Host" && 
                    header.Key != "Content-Length")
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Copy body for non-GET requests
            if (request.Method != "GET" && request.Method != "HEAD" && request.ContentLength > 0)
            {
                requestMessage.Content = new StreamContent(request.Body);
                if (request.ContentType != null)
                {
                    requestMessage.Content.Headers.TryAddWithoutValidation("Content-Type", request.ContentType);
                }
            }

            var response = await _httpClient.SendAsync(requestMessage);

            // Copy response status
            context.Response.StatusCode = (int)response.StatusCode;

            // Copy response headers
            foreach (var header in response.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in response.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // Copy response body
            await response.Content.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 502;
            await context.Response.WriteAsync($"Proxy error: {ex.Message}");
        }
    }
} 