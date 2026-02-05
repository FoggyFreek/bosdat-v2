using System.Net.Http.Headers;
using System.Net.Http.Json;
using BosDAT.Worker.Configuration;
using BosDAT.Worker.Models;
using Microsoft.Extensions.Options;

namespace BosDAT.Worker.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WorkerSettings _settings;
    private readonly ILogger<AuthenticatedHttpClientHandler> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public AuthenticatedHttpClientHandler(
        IHttpClientFactory httpClientFactory,
        IOptions<WorkerSettings> settings,
        ILogger<AuthenticatedHttpClientHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        await EnsureValidTokenAsync(cancellationToken);

        if (!string.IsNullOrEmpty(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized, attempting token refresh");
            await RefreshTokenAsync(cancellationToken);

            if (!string.IsNullOrEmpty(_accessToken))
            {
                var retryRequest = await CloneRequestAsync(request);
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
        }

        return response;
    }

    private async Task EnsureValidTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow.AddMinutes(5) < _tokenExpiresAt)
        {
            return;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow.AddMinutes(5) < _tokenExpiresAt)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_refreshToken))
            {
                await RefreshTokenAsync(cancellationToken);
            }
            else
            {
                await LoginAsync(cancellationToken);
            }
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Authenticating worker with API using email: {Email}", _settings.Credentials.Email);

        using var client = _httpClientFactory.CreateClient("BosApiAuth");
        var loginRequest = new LoginRequest
        {
            Email = _settings.Credentials.Email,
            Password = _settings.Credentials.Password
        };

        var response = await client.PostAsJsonAsync("api/auth/login", loginRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Worker authentication failed: {StatusCode} - {Error}", response.StatusCode, error);
            throw new InvalidOperationException($"Worker authentication failed: {response.StatusCode}");
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        if (authResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize auth response");
        }

        UpdateTokens(authResponse);
        _logger.LogInformation("Worker authenticated successfully, token expires at {ExpiresAt}", _tokenExpiresAt);
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_refreshToken))
        {
            await LoginAsync(cancellationToken);
            return;
        }

        _logger.LogDebug("Refreshing worker authentication token");

        using var client = _httpClientFactory.CreateClient("BosApiAuth");
        var refreshRequest = new RefreshRequest { RefreshToken = _refreshToken };

        var response = await client.PostAsJsonAsync("api/auth/refresh", refreshRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Token refresh failed, falling back to full login");
            _refreshToken = null;
            await LoginAsync(cancellationToken);
            return;
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        if (authResponse == null)
        {
            throw new InvalidOperationException("Failed to deserialize refresh response");
        }

        UpdateTokens(authResponse);
        _logger.LogDebug("Worker token refreshed, expires at {ExpiresAt}", _tokenExpiresAt);
    }

    private void UpdateTokens(AuthResponse authResponse)
    {
        _accessToken = authResponse.AccessToken;
        _refreshToken = authResponse.RefreshToken;
        _tokenExpiresAt = authResponse.ExpiresAt;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsStringAsync();
            clone.Content = new StringContent(content, System.Text.Encoding.UTF8, request.Content.Headers.ContentType?.MediaType ?? "application/json");
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
