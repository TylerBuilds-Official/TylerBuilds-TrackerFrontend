using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace JobTrackerFrontend.Services;

public class ApiClient
{
    
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    
    public ApiClient(AuthService authService)
    {
        _authService = authService;
        var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 20,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };
        _httpClient = new HttpClient(handler)
        {
            // Adjust to 'DevApiBaseUrl' during development if making changes to the API.
            // FIXME 
            BaseAddress = new Uri(AppConfig.DevApiBaseUrl),
            Timeout     = TimeSpan.FromSeconds(15)
        };
    }

    
    private async Task AttachTokenAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await AttachTokenAsync();
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        await AttachTokenAsync();
        var response = await _httpClient.PostAsJsonAsync(endpoint, body, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        await AttachTokenAsync();
        var response = await _httpClient.PutAsJsonAsync(endpoint, body, JsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    public async Task PutAsync<TRequest>(string endpoint, TRequest body)
    {
        await AttachTokenAsync();
        var response = await _httpClient.PutAsJsonAsync(endpoint, body, JsonOptions);
        response.EnsureSuccessStatusCode();
    }

    
    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest body)
    {
        await AttachTokenAsync();
        var content = JsonContent.Create(body, options: JsonOptions);
        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint) { Content = content };
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);
    }

    
    public async Task PatchAsync(string endpoint)
    {
        await AttachTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Patch, endpoint);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string endpoint)
    {
        await AttachTokenAsync();
        var response = await _httpClient.DeleteAsync(endpoint);
        response.EnsureSuccessStatusCode();
    }
}
