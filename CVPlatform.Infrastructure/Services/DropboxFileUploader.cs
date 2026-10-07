using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CVPlatform.Application.Support;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CVPlatform.Infrastructure.Services;

public class DropboxFileUploader : IFileUploader
{
    private readonly HttpClient _httpClient;
    private readonly DropboxOptions _options;
    private readonly ILogger<DropboxFileUploader> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _cachedAccessToken;
    private DateTime _tokenExpiresAtUtc = DateTime.MinValue;

    public DropboxFileUploader(
        HttpClient httpClient,
        IOptions<DropboxOptions> options,
        ILogger<DropboxFileUploader> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadJsonAsync(string fileName, string jsonContent, CancellationToken ct = default)
    {
        var appKey = _options.AppKey?.Trim() ?? string.Empty;
        var appSecret = _options.AppSecret?.Trim() ?? string.Empty;
        var refreshToken = _options.RefreshToken?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(appKey) ||
            string.IsNullOrWhiteSpace(appSecret) ||
            string.IsNullOrWhiteSpace(refreshToken) ||
            appKey.StartsWith("PUT_") ||
            appSecret.StartsWith("PUT_") ||
            refreshToken.StartsWith("PUT_"))
        {
            _logger.LogWarning("Dropbox credentials are not configured.");
            return new FileUploadResult(false, null, "DROPBOX_NOT_CONFIGURED");
        }

        var folder = string.IsNullOrWhiteSpace(_options.FolderPath) ? "/CVPlatform/SupportTickets" : _options.FolderPath.Trim().TrimEnd('/');
        var remotePath = $"{folder}/{fileName}";

        var token = await GetValidAccessTokenAsync(forceRefresh: false, ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new FileUploadResult(false, null, "DROPBOX_AUTH_FAILED");
        }

        var result = await ExecuteUploadAsync(remotePath, jsonContent, token, ct);
        if (!result.Success && result.ErrorCode == "UNAUTHORIZED")
        {
            token = await GetValidAccessTokenAsync(forceRefresh: true, ct);
            if (string.IsNullOrWhiteSpace(token))
            {
                return new FileUploadResult(false, null, "DROPBOX_AUTH_FAILED");
            }

            result = await ExecuteUploadAsync(remotePath, jsonContent, token, ct);
        }

        return result;
    }

    private async Task<FileUploadResult> ExecuteUploadAsync(string remotePath, string jsonContent, string accessToken, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var apiArgJson = JsonSerializer.Serialize(new
            {
                path = remotePath,
                mode = "add",
                autorename = true,
                mute = true
            });
            request.Headers.Add("Dropbox-API-Arg", apiArgJson);

            var contentBytes = Encoding.UTF8.GetBytes(jsonContent);
            request.Content = new ByteArrayContent(contentBytes);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            using var response = await _httpClient.SendAsync(request, ct);

            if (response.IsSuccessStatusCode)
            {
                return new FileUploadResult(true, remotePath);
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Dropbox upload failed with status code {StatusCode}: {ErrorBody}", (int)response.StatusCode, errorBody);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return new FileUploadResult(false, null, "UNAUTHORIZED");
            }

            return new FileUploadResult(false, null, "DROPBOX_UPLOAD_FAILED");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Dropbox upload.");
            return new FileUploadResult(false, null, "DROPBOX_NETWORK_ERROR");
        }
    }

    private async Task<string?> GetValidAccessTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (!forceRefresh && !string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiresAtUtc)
            {
                return _cachedAccessToken;
            }

            var appKey = _options.AppKey?.Trim() ?? string.Empty;
            var appSecret = _options.AppSecret?.Trim() ?? string.Empty;
            var refreshToken = _options.RefreshToken?.Trim() ?? string.Empty;

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token");

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = appKey,
                ["client_secret"] = appSecret
            };
            request.Content = new FormUrlEncodedContent(form);

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Dropbox token refresh failed with status code {StatusCode}: {ErrorBody}", (int)response.StatusCode, errorBody);
                _cachedAccessToken = null;
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            if (root.TryGetProperty("access_token", out var tokenProp))
            {
                var token = tokenProp.GetString();
                var expiresIn = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 14400;
                _cachedAccessToken = token;
                _tokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, expiresIn - 120));
                return _cachedAccessToken;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Dropbox access token.");
            _cachedAccessToken = null;
            return null;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
